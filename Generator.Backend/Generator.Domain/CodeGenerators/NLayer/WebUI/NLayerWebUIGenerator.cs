using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Core;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository;
using Generator.Domain.CodeGenerators.Helpers;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Generator.Domain.CodeGenerators.NLayer.WebUI;

public class NLayerWebUIGenerator : IGenerationStep
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly RelationRepository _relationRepository;

    private readonly FileSystemService _fs;
    private readonly RoslynSyntaxHelper _roslyn;
    private readonly DotnetCliService _cli;
    private readonly TemplateRenderer _templateRenderer;

    private AppSetting _appSetting = null!;

    public string Name => "WebUI Layer";
    public int Order => 6;
    public int ProgressWeight => 15;

    public NLayerWebUIGenerator(
        EntityRepository entityRepository,
        FieldRepository fieldRepository,
        DtoRepository dtoRepository,
        RelationRepository relationRepository,
        FileSystemService fs,
        RoslynSyntaxHelper roslyn,
        DotnetCliService cli, TemplateRenderer templateRenderer)
    {
        _entityRepository = entityRepository;
        _fieldRepository = fieldRepository;
        _dtoRepository = dtoRepository;
        _relationRepository = relationRepository;
        _fs = fs;
        _roslyn = roslyn;
        _cli = cli;
        _templateRenderer = templateRenderer;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            _appSetting = appSetting;
            log(CreateProject());
            log(_cli.AddPackage(_appSetting, "FluentValidation.AspNetCore --version 11.3.1", _appSetting.WebUILayerProjectName));
            log(_cli.AddPackage(_appSetting, "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation --version 10.0.4", _appSetting.WebUILayerProjectName));
            log(_cli.AddPackage(_appSetting, "Microsoft.EntityFrameworkCore.Design --version 10.0.4", _appSetting.WebUILayerProjectName));
            log(_cli.Restore(_appSetting, _appSetting.WebUILayerProjectName));
            log(_templateRenderer.GenerateStaticFiles(_appSetting, "WebUI", _appSetting.WebUILayerProjectName));
            log(GenerateSideMenuViewComponent());
            log(GenerateViewModels());
            log(GenerateControllers());
            log(GenerateViews());
            log(Copywwwroot());
            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public string CreateProject()
    {
        try
        {
            string layerPath = Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{_appSetting.WebUILayerProjectName}.csproj");

            if (Directory.Exists(layerPath) && File.Exists(csprojPath))
                return "INFO: WebUI layer project already exists.";


            _cli.RunCommand(_appSetting.SolutionPath, "dotnet", $"new mvc -n {_appSetting.WebUILayerProjectName}");

            bool isSlnx = File.Exists(Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.slnx"));

            _cli.RunCommand(_appSetting.SolutionPath, "dotnet", $"sln {_appSetting.SolutionName}.{(isSlnx ? "slnx" : "sln")} add {_appSetting.WebUILayerProjectName}/{_appSetting.WebUILayerProjectName}.csproj");
            _cli.RunCommand(layerPath, "dotnet", $"add reference ../{_appSetting.BusinessLayerProjectName}/{_appSetting.BusinessLayerProjectName}.csproj");

            _fs.RemoveFile(layerPath, "Program.cs");
            _fs.RemoveFile(layerPath, "appsettings.json");

            string projectViewsPath = Path.Combine(layerPath, "Views");
            _fs.RemoveFile(projectViewsPath, "_ViewImports.cshtml");

            string projectViewsSharedPath = Path.Combine(layerPath, "Views", "Shared");
            _fs.RemoveFile(projectViewsSharedPath, "_Layout.cshtml");
            _fs.RemoveFile(projectViewsSharedPath, "_Layout.cshtml.css");
            _fs.RemoveFile(projectViewsSharedPath, "Error.cshtml");

            _fs.RemoveFolder(Path.Combine(layerPath, "Models"));
            _fs.RemoveFolder(Path.Combine(layerPath, "Controllers"));

            return "OK: WebUI Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the WebUI project. \n\t Details:{ex.Message}");
        }
    }

    public string GetIdentityRegistrationCode()
    {
        if (!_appSetting.IsThereIdentity)
            return string.Empty;

        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string identityUserType = identityTypeConfigs.IdentityUserType;
        string identityRoleType = identityTypeConfigs.IdentityRoleType;

        return $@"
            #region ------- IDENTITY -------
            TokenSettings tokenSettings = builder.Configuration.GetSection(""TokenSettings"").Get<TokenSettings>() ?? new();
            builder.Services.AddSingleton(tokenSettings);

            builder.Services
                .AddIdentity<{identityUserType}, {identityRoleType}>(options =>
                {{
                    // Default Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    options.SignIn.RequireConfirmedEmail = false;

                    options.Password.RequiredLength = 4;
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;

                    options.User.RequireUniqueEmail = false;
                    options.User.AllowedUserNameCharacters = ""abcÃ§defgÄŸhiÄ±jklmnoÃ¶pqrsÅŸtuÃ¼vwxyzABCÃ‡DEFGÄHIÄ°JKLMNOÃ–PQRSÅTUÃœVWXYZ0123456789-._@+/*|!,;:()&#?[] "";
                }})
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthorization();
            #endregion
        ";
    }

    #region SideMenuViewComponent
    public string GenerateSideMenuViewComponent()
    {
        List<Entity> entities = _entityRepository.GetAll();
        entities = entities.OrderBy(e => e.Name).ToList();

        var code = _roslyn.CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Mvc",
                $"{_appSetting.WebUILayerProjectName}.Models.UI",
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{_appSetting.WebUILayerProjectName}.ViewComponents",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SideMenuViewComponent",
                        baseTypes: [SyntaxFactory.ParseTypeName("ViewComponent")],
                        members: [
                            GenerateInvokeMethod(entities),
                            _roslyn.MethodDeclaration(
                                modifiers: [SyntaxKind.PrivateKeyword],
                                returnType: "bool",
                                name: "HandleActiveMenu",
                                parameters: [
                                    _roslyn.ParameterDeclaration("MenuItem", "item"),
                                    _roslyn.ParameterDeclaration("string", "currentPath")
                                ],
                                body: @"
                                    bool isActive = !string.IsNullOrWhiteSpace(item.Path) && (currentPath.Equals(item.Path, StringComparison.OrdinalIgnoreCase) || currentPath.StartsWith(item.Path + ""/"", StringComparison.OrdinalIgnoreCase));
                                    bool hasActiveChild = false;

                                    if (item.SubMenuItems != null)
                                    {
                                        foreach (var child in item.SubMenuItems)
                                        {
                                            if (HandleActiveMenu(child, currentPath))
                                                hasActiveChild = true;
                                        }
                                    }

                                    item.IsActive = isActive;
                                    item.HasActiveChild = hasActiveChild;

                                    return isActive || hasActiveChild;
                                "
                            )
                        ]
                    )
                ]
            )
        );


        string folderPathMenuItem = System.IO.Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName, "ViewComponents");
        return _fs.AddFile(folderPathMenuItem, "SideMenuViewComponent.cs", code.ToFullString());
    }

    private MethodDeclarationSyntax GenerateInvokeMethod(List<Entity> entities)
    {
        List<ObjectCreationExpressionSyntax> subMenuItems = new();
        foreach (Entity entity in entities)
        {
            subMenuItems.Add(
                _roslyn.ObjectCreation(
                    typeName: "MenuItem",
                    members: [
                        _roslyn.PropertyAssignment(name: "Title", value: $"\"{entity.Name}\""),
                        _roslyn.PropertyAssignment(name: "Icon", value: "\"<i class=\\\"ki-duotone ki-right text-gray-900 fs-2tx\\\"></i>\""),
                        _roslyn.PropertyAssignment(name: "Path", value:  $"\"/{entity.Name}/Index\""),
                    ]
                )
            );
        }
        var menuItems = _roslyn.LocalDeclaration(
            type: "var",
            name: "menuItems",
            expression: _roslyn.ObjectCreationCollection(
                typeName: "List<MenuItem>",
                items: [
                    _roslyn.ObjectCreation(
                        typeName: "MenuItem",
                        members: [
                            _roslyn.PropertyAssignment(name: "Title", value: "\"Dashboard\""),
                            _roslyn.PropertyAssignment(name: "Icon", value: "\"<i class=\\\"ki-duotone ki-element-11 fs-2\\\"><span class=\\\"path1\\\"></span><span class=\\\"path2\\\"></span><span class=\\\"path3\\\"></span><span class=\\\"path4\\\"></span></i>\""),
                            _roslyn.PropertyAssignment(name: "Path", value: "\"/Home/Index\"")
                        ]
                    ),
                    _roslyn.ObjectCreation(
                        typeName: "MenuItem",
                        members: [
                            _roslyn.PropertyAssignment(name: "Title", value: "\"Pages\""),
                            _roslyn.PropertyAssignment(name: "Icon", value: "\"<i class=\\\"fa-regular fa-folder-open\\\"></i>\""),
                            _roslyn.PropertyAssignment(name: "GroupName", value: "\"Pages\""),
                            _roslyn.PropertyAssignment(
                                name: "SubMenuItems",
                                expression: _roslyn.ObjectCreationCollection(
                                    typeName: "List<MenuItem>",
                                    items: [
                                        ..subMenuItems
                                    ]
                                )
                            ),
                        ]
                    )
                ]
            )
        );

        return _roslyn.MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword],
            returnType: "IViewComponentResult",
            name: "Invoke",
            block: SyntaxFactory.Block(
                menuItems,
                SyntaxFactory.ParseStatement(@"
                    string currentPath = (HttpContext.Request.Path.Value ?? string.Empty).TrimEnd('/');
                    foreach (var menu in menuItems)
                    {
                        HandleActiveMenu(menu, currentPath);
                    }
                    return View(menuItems);
                ")
            )
        );
    }
    #endregion

    #region ViewModels
    public string GenerateViewModels()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        foreach (var entity in entities)
        {
            string folderPath = System.IO.Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName, "Models", "ViewModels", $"{entity.Name}");

            results.Add(_fs.AddFile(folderPath, $"{entity.Name}ViewModel.cs", GenerateViewModelIndex(entity)));
            results.Add(_fs.AddFile(folderPath, $"{entity.Name}CreateViewModel.cs", GenerateViewModelCreate(entity)));
            results.Add(_fs.AddFile(folderPath, $"{entity.Name}UpdateViewModel.cs", GenerateViewModelUpdate(entity)));
        }

        return string.Join("\n", results);
    }

    private string GenerateViewModelIndex(Entity entity)
    {
        #region ViewModel Properties
        var vmPropList = new List<PropertyDeclarationSyntax>();
        // SelectList Props
        List<Field> filterableFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.Filterable, include: i => i.Include(x => x.FieldType));
        foreach (var field in filterableFields)
        {
            Relation? relation = _relationRepository.Get(
                filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
            );
            if (relation == null) continue;

            vmPropList.Add(_roslyn.PropertyDeclaration("SelectList", field.Name.Pluralize(), false, [SyntaxKind.PublicKeyword]));
        }
        // FilterModel Prop
        if (filterableFields.Any() || entity.SoftDeletable)
            vmPropList.Add(_roslyn.PropertyDeclaration($"{entity.Name}FilterModel", "FilterModel", true, [SyntaxKind.PublicKeyword], earlyInstance: true));
        #endregion

        #region FilterModel Properties
        var filerModelProperties = new List<MemberDeclarationSyntax>();
        foreach (var field in filterableFields)
            filerModelProperties.Add(_roslyn.PropertyDeclaration($"{field.GetMapedTypeName()}", field.Name, false, [SyntaxKind.PublicKeyword]));
        if (entity.SoftDeletable)
            filerModelProperties.Add(_roslyn.PropertyDeclaration("bool", "IsDeleted", false, [SyntaxKind.PublicKeyword]));
        #endregion

        var code = _roslyn.CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Mvc.Rendering",
                $"{_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}",
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entity.Name}ViewModel",
                        members: [..vmPropList]
                    ),
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entity.Name}FilterModel",
                        members: [..filerModelProperties]
                    )
                ]
            )
        );

        return code.ToFullString();
    }
    public string GenerateViewModelCreate(Entity entity)
    {
        Dto? createDto = _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(y => y.SourceField).ThenInclude(y => y.FieldType));
        bool isThereCreateDto = createDto != default;
        string createModelType = isThereCreateDto ? createDto!.Name : $"{_appSetting.ModelLayerProjectName}.Entities.{entity.Name}";

        // Property List
        var propertyList = new List<MemberDeclarationSyntax>
        {
            _roslyn.PropertyDeclaration(createModelType, "CreateModel", true, [SyntaxKind.PublicKeyword], earlyInstance: true)
        };

        // SelectList Props
        if (isThereCreateDto)
        {
            foreach (var dtoField in createDto!.DtoFields.Where(df => df.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
            {
                Relation? relation = _relationRepository.Get(
                    filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                    include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                propertyList.Add(_roslyn.PropertyDeclaration("SelectList", dtoField.Name.Pluralize(), false, [SyntaxKind.PublicKeyword]));
            }
        }
        else
        {
            List<Field> baseFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType), enableTracking: false);
            foreach (var field in baseFields)
            {
                Relation? relation = _relationRepository.Get(
                    filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                    include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                propertyList.Add(_roslyn.PropertyDeclaration("SelectList", field.Name.Pluralize(), false, [SyntaxKind.PublicKeyword]));
            }
        }

        return _roslyn.CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Mvc.Rendering",
                isThereCreateDto ? $"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands" : string.Empty,
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entity.Name}CreateViewModel",
                        members: [.. propertyList]
                    )
                ]
            )
        ).ToFullString();
    }
    public string GenerateViewModelUpdate(Entity entity)
    {
        Dto? updateDto = _dtoRepository.Get(f => f.Id == entity.UpdateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(y => y.SourceField).ThenInclude(y => y.FieldType));
        bool isThereUpdateDto = updateDto != default;
        string updateModelType = isThereUpdateDto ? updateDto!.Name : $"{_appSetting.ModelLayerProjectName}.Entities.{entity.Name}";

        // Property List
        var propertyList = new List<MemberDeclarationSyntax>
        {
            _roslyn.PropertyDeclaration(updateModelType, "UpdateModel", true, [SyntaxKind.PublicKeyword], earlyInstance: true)
        };

        // SelectList Props
        if (isThereUpdateDto)
        {
            foreach (var dtoField in updateDto!.DtoFields.Where(df => df.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
            {
                Relation? relation = _relationRepository.Get(
                    filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                    include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                propertyList.Add(_roslyn.PropertyDeclaration("SelectList?", dtoField.Name.Pluralize(), false, [SyntaxKind.PublicKeyword]));
            }
        }
        else
        {
            List<Field> baseFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType));
            foreach (var field in baseFields)
            {
                Relation? relation = _relationRepository.Get(
                    filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                    include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                propertyList.Add(_roslyn.PropertyDeclaration("SelectList?", field.Name.Pluralize(), false, [SyntaxKind.PublicKeyword]));
            }
        }

        return _roslyn.CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Mvc.Rendering",
                isThereUpdateDto ? $"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands" : string.Empty,
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entity.Name}UpdateViewModel",
                        members: [.. propertyList]
                    )
                ]
            )
        ).ToFullString();
    }
    #endregion

    #region Controllers
    public string GenerateControllers()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));
        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(x => x.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields)
            );

            Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(y => y.SourceField).ThenInclude(y => y.FieldType)) : default;
            bool isThereCreateDto = createDto != default;
            Dto? updateDto = entity.UpdateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.UpdateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(y => y.SourceField).ThenInclude(y => y.FieldType)) : default;
            bool isThereUpdateDto = updateDto != default;

            List<string> relationalEntities = new List<string>() { entity.Name };

            Dictionary<string, string> selectableRelations_index = new Dictionary<string, string>(); // field name of foreign entity(this entity), field name of primary entity
            Dictionary<string, string> selectableRelations_create = new Dictionary<string, string>();
            Dictionary<string, string> selectableRelations_update = new Dictionary<string, string>();

            #region RelationalEntities && SelectableRelations
            // Selectable Relations for Index Action Method
            List<Field> filterableFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.Filterable && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType), enableTracking: false);
            foreach (var field in filterableFields)
            {
                Relation? relation = _relationRepository.Get(
                  filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                  include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                selectableRelations_index.Add(field.Name, relation.PrimaryField.Entity.Name);
                if (!relationalEntities.Contains(relation.PrimaryField.Entity.Name))
                    relationalEntities.Add(relation.PrimaryField.Entity.Name);
            }

            // Selectable Relations for Create Action Method
            if (isThereCreateDto)
            {
                foreach (var dtoField in createDto!.DtoFields.Where(df => df.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
                {
                    Relation? relation = _relationRepository.Get(
                        filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                        include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                    );
                    if (relation == null) continue;

                    selectableRelations_create.Add(dtoField.Name, relation.PrimaryField.Entity.Name);
                    if (!relationalEntities.Contains(relation.PrimaryField.Entity.Name))
                        relationalEntities.Add(relation.PrimaryField.Entity.Name);
                }
            }
            else
            {
                List<Field> baseFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType), enableTracking: false);
                foreach (var field in baseFields)
                {
                    Relation? relation = _relationRepository.Get(
                        filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                        include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                    );
                    if (relation == null) continue;

                    selectableRelations_create.Add(field.Name, relation.PrimaryField.Entity.Name);
                    if (!relationalEntities.Contains(relation.PrimaryField.Entity.Name))
                        relationalEntities.Add(relation.PrimaryField.Entity.Name);
                }
            }

            // Selectable Relations for Update Action Method
            if (isThereUpdateDto)
            {
                foreach (var dtoField in updateDto!.DtoFields.Where(df => df.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
                {
                    Relation? relation = _relationRepository.Get(
                        filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                        include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                    );
                    if (relation == null) continue;

                    selectableRelations_update.Add(dtoField.Name, relation.PrimaryField.Entity.Name);
                    if (!relationalEntities.Contains(relation.PrimaryField.Entity.Name))
                        relationalEntities.Add(relation.PrimaryField.Entity.Name);
                }
            }
            else
            {
                List<Field> baseFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType), enableTracking: false);
                foreach (var field in baseFields)
                {
                    Relation? relation = _relationRepository.Get(
                        filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                        include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                    );
                    if (relation == null) continue;

                    selectableRelations_update.Add(field.Name, relation.PrimaryField.Entity.Name);
                    if (!relationalEntities.Contains(relation.PrimaryField.Entity.Name))
                        relationalEntities.Add(relation.PrimaryField.Entity.Name);
                }
            }
            #endregion

            #region Usings
            List<string> usings = new()
            {
                "Microsoft.AspNetCore.Mvc",
                $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",
                $"{_appSetting.ModelLayerProjectName}.Entities",
                $"{_appSetting.BusinessLayerProjectName}.Abstract",
                $"{_appSetting.WebUILayerProjectName}.Controllers.Base",
                $"{_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}"
            };
            if (dtos.Any(f => f.CrudTypeId != (byte)Enums.CrudTypeEnums.Read))
                usings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands");
            if (dtos.Any(f => f.CrudTypeId == (byte)Enums.CrudTypeEnums.Read))
                usings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Queries");
            #endregion


            #region Fields
            List<FieldDeclarationSyntax> fields =
            [
                .. relationalEntities.Select(e => _roslyn.FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], $"I{e}Service", $"_{e.ToCamelCase()}Service")),
            ];
            #endregion

            #region Constructor Parameters
            List<ParameterSyntax> constructorParams =
            [
                _roslyn.ParameterDeclaration($"ILogger<{entity.Name}Controller>", "logger"),
                .. relationalEntities.Select(e => _roslyn.ParameterDeclaration($"I{e}Service", $"{e.ToCamelCase()}Service"))
            ];
            #endregion


            #region Constructor Statement Expressions
            List<StatementSyntax> constructorStatements =
            [
                .. relationalEntities.Select(e => _roslyn.StatementExpression($"_{e.ToCamelCase()}Service", $"{e.ToCamelCase()}Service"))
            ];
            #endregion

            var code_controller = _roslyn.CompilationUnit(
                usings: [.. usings],
                nspace: _roslyn.NamespaceDeclaration(
                    value: $"{_appSetting.WebUILayerProjectName}.Controllers",
                    members: [
                        _roslyn.ClassDeclaration(
                            name: $"{entity.Name}Controller",
                            modifiers: [SyntaxKind.PublicKeyword],
                            baseTypes: [SyntaxFactory.ParseTypeName("BaseController")],
                            members: [
                                ..fields,
                                _roslyn.ConstructorDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword],
                                    name: $"{entity.Name}Controller",
                                    parameters: [
                                        ..constructorParams
                                    ],
                                    baseArgs: ["logger"],
                                    statements: [..constructorStatements]
                                ),
                                ..GenerateControllerMethods(entity, dtos, selectableRelations_index, selectableRelations_create, selectableRelations_update)
                            ]
                        )
                    ]
                )
            );

            string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName, "Controllers");
            results.Add(_fs.AddFile(folderPath, $"{entity.Name}Controller.cs", code_controller.ToFullString()));
        }

        return string.Join("\n", results);
    }

    private List<MethodDeclarationSyntax> GenerateControllerMethods(Entity entity, List<Dto> dtos, Dictionary<string, string> selectableRelations_index, Dictionary<string, string> selectableRelations_create, Dictionary<string, string> selectableRelations_update)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).ToList();
        var uniqueFieldParameters = uniqueFields.Select(f => _roslyn.ParameterDeclaration(f.GetMapedTypeName(), f.Name.ToCamelCase(), true)).ToList();

        string methodUniqueArgs = string.Join(", ", uniqueFields.Select(f => $"{f.Name.ToCamelCase()}: {f.Name.ToCamelCase()}"));

        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        #region INDEX
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Index",
            returnType: "Task<IActionResult>",
            block: SyntaxFactory.Block(
                new List<StatementSyntax>(
                [
                    ..selectableRelations_index.Select(sr => _roslyn.LocalDeclaration("var", sr.Key.ToCamelCase().Pluralize(), _roslyn.ExpressionStatement($"_{sr.Value.ToCamelCase()}Service.SelectListAsync()", true))),
                    _roslyn.LocalDeclaration(
                        type: "var",
                        name: "viewModel",
                        expression: _roslyn.ObjectCreation(
                            typeName: $"{entity.Name}ViewModel",
                            members: [
                                ..selectableRelations_index.Select(sr =>
                                    _roslyn.PropertyAssignment(
                                        sr.Key.Pluralize(),
                                        $"{sr.Key.ToCamelCase().Pluralize()}.Data"
                                    )
                                )
                            ]
                        )
                    ),
                    _roslyn.ReturnStatement("View(viewModel)")
                ])
            )
        ));
        #endregion

        #region CREATE
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Create",
            returnType: "Task<IActionResult>",
            block: SyntaxFactory.Block(
                new List<StatementSyntax>(
                [
                    ..selectableRelations_create.Select(sr => _roslyn.LocalDeclaration("var", sr.Key.ToCamelCase().Pluralize(), _roslyn.ExpressionStatement($"_{sr.Value.ToCamelCase()}Service.SelectListAsync()", true))),
                    _roslyn.LocalDeclaration(
                        type: "var",
                        name: "viewModel",
                        expression: _roslyn.ObjectCreation(
                            typeName: $"{entity.Name}CreateViewModel",
                            members: [
                                ..selectableRelations_create.Select(sr =>_roslyn.PropertyAssignment(sr.Key.Pluralize(), $"{sr.Key.ToCamelCase().Pluralize()}.Data"))
                            ]
                        )
                    ),
                    _roslyn.ReturnStatement("PartialView(\"./Partials/CreateForm\", viewModel)")
                ])
            )
        ));

        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Create",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration(createDto?.Name ?? entity.Name, "request", true)
            ],
            body: $@"
                var result = await {serviceName}.CreateAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        #region UPDATE
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Update",
            returnType: "Task<IActionResult>",
            parameters: [
                ..uniqueFieldParameters
            ],
            block: SyntaxFactory.Block(
                new List<StatementSyntax>(
                [
                    SyntaxFactory.ParseStatement(@$"
                        var result = await {serviceName}.{(updateDto != null ? "GetUpdateModelAsync" : "GetAsync")}({methodUniqueArgs});
                        if (!result.IsSuccess) return ToAction(result);
                    "),
                    ..selectableRelations_update.Select(sr => _roslyn.LocalDeclaration("var", sr.Key.ToCamelCase().Pluralize(), _roslyn.ExpressionStatement($"_{sr.Value.ToCamelCase()}Service.SelectListAsync()", true))),
                    _roslyn.LocalDeclaration(
                        type: "var",
                        name: "viewModel",
                        expression: _roslyn.ObjectCreation(
                            typeName: $"{entity.Name}UpdateViewModel",
                            members: [
                                _roslyn.PropertyAssignment("UpdateModel", "result.Data"),
                                ..selectableRelations_update.Select(sr => _roslyn.PropertyAssignment(sr.Key.Pluralize(), $"{sr.Key.ToCamelCase().Pluralize()}.Data"))
                            ]
                        )
                    ),
                    _roslyn.ReturnStatement("PartialView(\"./Partials/UpdateForm\", viewModel)")
                ])
            )
        ));

        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Update",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration(updateDto?.Name ?? entity.Name, "updateModel", true)
            ],
            body: $@"
                var result = await {serviceName}.UpdateAsync(updateModel);
                return ToAction(result);
            "
        ));
        #endregion

        #region DELETE
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        if (deleteDto != null)
        {
            methods.Add(_roslyn.MethodDeclaration(
                attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "Delete",
                returnType: "Task<IActionResult>",
                parameters:
                [
                    _roslyn.ParameterDeclaration(deleteDto.Name, "deleteModel", true)
                ],
                body: $@"
                    var result = await {serviceName}.DeleteAsync(deleteModel);
                    return ToAction(result);
                "
            ));
        }
        else
        {
            methods.Add(_roslyn.MethodDeclaration(
                attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "Delete",
                returnType: "Task<IActionResult>",
                parameters:
                [
                    ..uniqueFieldParameters
                ],
                body: $@"
                    var result = await {serviceName}.DeleteAsync({methodUniqueArgs});
                    return ToAction(result);
                "
            ));
        }
        #endregion

        #region RESTORE
        if (entity.SoftDeletable)
        {
            methods.Add(_roslyn.MethodDeclaration(
                attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "Restore",
                returnType: "Task<IActionResult>",
                parameters: [
                    ..uniqueFieldParameters
                ],
                body: $@"
                    var result = await {serviceName}.RestoreAsync({methodUniqueArgs});
                    return ToAction(result);
                "
            ));
        }
        #endregion

        #region DATATABLE 
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableClientSide",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration("DynamicDatatableRequest", "request", true)
            ],
            body: $@"
                var result = await {serviceName}.DatatableClientSideAsync(request);
                return ToAction(result);
            "
        ));
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableServerSide",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration("DynamicDatatableRequest", "request", true)
            ],
            body: $@"
                var result = await {serviceName}.DatatableServerSideAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        return methods;
    }
    #endregion

    #region Views
    public string GenerateViews()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            string pathView = Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName, "Views", entity.Name);
            string pathPartial = Path.Combine(_appSetting.SolutionPath, _appSetting.WebUILayerProjectName, "Views", entity.Name, "Partials");

            results.Add(_fs.AddFile(pathView, $"Index.cshtml", IndexHtml(entity)));
            results.Add(_fs.AddFile(pathPartial, $"CreateForm.cshtml", FormHtml(entity, false)));
            results.Add(_fs.AddFile(pathPartial, $"UpdateForm.cshtml", FormHtml(entity, true)));
        }
        return string.Join("\n", results);
    }

    public string IndexHtml(Entity entity)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base, include: i => i.Include(x => x.FieldType), enableTracking: false);
        var filterableFields = fieldList.Where(f => f.Filterable).ToList();

        List<(string fieldName, (int inputType, int inputKind, string inputCode) data)> filterInputs = new List<(string, (int, int, string))>();

        Dto? reportDto = entity.ReportDtoId != default ? _dtoRepository.Get(f => f.Id == entity.ReportDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField.FieldType)) : default;
        bool isThereReportDto = reportDto != default;

        var selectableRelations = new Dictionary<string, string>(); // field name of foreign entity(this entity), field name of primary entity
        foreach (var field in filterableFields)
        {
            Relation? relation = _relationRepository.Get(
              filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
              include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
            );
            if (relation == null) continue;

            selectableRelations.Add(field.Name, relation.PrimaryField.Entity.Name);
        }

        #region Form Inputs
        StringBuilder codeFilterForm = new StringBuilder();
        foreach (var field in filterableFields)
        {
            int inptType = field.GetVariableGroup(selectableRelations);
            int inptKind = field.GetDatatableConditionKind(inptType);
            string inptCode = HtmlInputGenerator.CreateInputHTML(field, inptType);
            codeFilterForm.Append(inptCode);
            filterInputs.Add((field.Name, (inptType, inptKind, inptCode)));
        }
        if (entity.SoftDeletable)
        {
            codeFilterForm.Append(@"
                    <div class=""mb-10"">
                        <div class=""form-check form-switch form-switch-sm form-check-custom form-check-solid"">
                            <input name=""IsDeleted"" value=""false"" type=""hidden"" />
                            <input name=""IsDeleted"" value=""true"" type=""checkbox"" class=""form-check-input"" />
                            <span class=""form-check-label fs-sm"">Include Deleted</span>
                        </div>
                    </div>
            ");
        }
        #endregion

        #region Table Header Columns
        StringBuilder tableHeaderColumns = new StringBuilder();
        if (isThereReportDto)
        {
            foreach (var dtoField in reportDto!.DtoFields.Where(f => !f.SourceField.IsUnique && f.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
                tableHeaderColumns.AppendLine($"\t\t\t\t\t<th>{dtoField.Name.DivideToLabelName()}</th>");
        }
        else
        {
            foreach (var field in fieldList.Where(f => !f.IsUnique))
                tableHeaderColumns.AppendLine($"\t\t\t\t\t<th>{field.Name.DivideToLabelName()}</th>");
        }

        if (entity.Auditable)
        {
            tableHeaderColumns.AppendLine("\t\t\t\t\t<th>Create Date</th>");
            tableHeaderColumns.AppendLine("\t\t\t\t\t<th>Last Update Date</th>");
        }
        if (entity.SoftDeletable)
        {
            tableHeaderColumns.AppendLine("\t\t\t\t\t<th>Status</th>");
            tableHeaderColumns.AppendLine("\t\t\t\t\t<th>Delete Date</th>");
        }
        tableHeaderColumns.AppendLine("\t\t\t\t\t<th>Actions</th>");
        #endregion

        #region Datatable Filter Request Model
        List<string> tempDataTableFilters = new List<string>();
        foreach (var filterInput in filterInputs)
        {
            if (filterInput.data.inputKind == 1) // Select
            {
                tempDataTableFilters.Add($@" 
                            {{
                                operator: 'eq',
                                field: '{filterInput.fieldName}',
                                value: $(""select[name='{filterInput.fieldName}']"").val()
                            }}
                ");
            }
            else if (filterInput.data.inputKind == 2) // Equals
            {
                tempDataTableFilters.Add($@" 
                            {{
                                operator: 'eq',
                                field: '{filterInput.fieldName}',
                                value: $(""input[name='{filterInput.fieldName}']"").val()
                            }}
                ");
            }
            else if (filterInput.data.inputKind == 3) // Contains
            {
                tempDataTableFilters.Add($@" 
                            {{
                                operator: 'contains',
                                field: '{filterInput.fieldName}',
                                value: $(""input[name='{filterInput.fieldName}']"").val()
                            }}
                ");
            }
            else if (filterInput.data.inputKind == 4) // CheckBox
            {
                tempDataTableFilters.Add($@" 
                            {{
                                operator: 'eq',
                                field: '{filterInput.fieldName}',
                                value: $('input[name=""{filterInput.fieldName}""]:checked').prop(""checked"") || false,
                            }}
                ");
            }
        }
        if (entity.SoftDeletable)
        {
            tempDataTableFilters.Add($@" 
                            {{
                                operator: 'base',
                                logic: 'or',
                                filters: [
                                    {{
                                        operator: 'eq',
                                        field: 'isDeleted',
                                        value: false,
                                    }},
                                    {{
                                        operator: 'eq',
                                        field: 'isDeleted',
                                        value: $('input[name=""IsDeleted""]:checked').prop(""checked"") || false,
                                    }}
                                ]
                            }}
            ");
        }
        string codeDataTableFilters = string.Join(",", tempDataTableFilters);

        string codeDatatableRequestData = $@"
                requestData: {{
                    filter: {{
                        operator: 'base',
                        logic: 'and',
                        filters: [
                            {codeDataTableFilters}
                        ]
                    }}
                }},
        ";
        #endregion

        #region DataTable Columns
        StringBuilder codeDatatableColumns = new StringBuilder();

        if (isThereReportDto)
        {
            foreach (var dtoField in reportDto!.DtoFields.Where(f => !f.SourceField.IsUnique && f.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
            {
                if (dtoField.SourceField.FieldTypeId == (byte)Enums.FieldTypeEnums.DateTime)
                {
                    codeDatatableColumns.Append($@"
                    {{
                        data: '{dtoField.Name}',
                        render: function (data) {{
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }}
                    }},");
                }
                else if (dtoField.SourceField.FieldTypeId == (byte)Enums.FieldTypeEnums.Bool)
                {
                    codeDatatableColumns.Append($@"
                    {{
                        data: '{dtoField.Name}',
                        render: function (data) {{
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }}
                    }},");
                }
                else
                {
                    codeDatatableColumns.AppendLine($"\t\t\t\t\t{{ data: '{dtoField.Name}' }},");
                }
            }
        }
        else
        {
            foreach (var field in fieldList.Where(f => !f.IsUnique))
            {
                if (field.FieldTypeId == (byte)Enums.FieldTypeEnums.DateTime)
                {
                    codeDatatableColumns.Append($@"
                    {{
                        data: '{field.Name}',
                        render: function (data) {{
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }}
                    }},");
                }
                else if (field.FieldTypeId == (byte)Enums.FieldTypeEnums.Bool)
                {
                    codeDatatableColumns.Append($@"
                    {{
                        data: '{field.Name}',
                        render: function (data) {{
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }}
                    }},");
                }
                else
                {
                    codeDatatableColumns.AppendLine($"\t\t\t\t\t{{ data: '{field.Name}' }},");
                }
            }
        }

        if (entity.Auditable)
        {
            codeDatatableColumns.Append(@"
                    {
                        data: 'CreateDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },
                    {
                        data: 'UpdateDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }
        if (entity.SoftDeletable)
        {
            codeDatatableColumns.Append(@"
                    {
                        data: 'isDeleted',
                        render: function (data) {
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }
                    },
                    {
                        data: 'deletedDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }

        // Action Button
        string uniqueFieldParams = string.Join(", ", entity.Fields.Where(f => f.IsUnique).Select(d => $"\"{d.Name.ToCamelCase()}\": rowData.{d.Name.ToCamelCase()}"));
        if (entity.SoftDeletable)
        {
            codeDatatableColumns.Append($@"
                    {{
                        data: null,
                        defaultContent: '',
                        searchable: false,
                        createdCell: function (td, cellData, rowData, row, col)
                        {{
                            let deleteHandleButton = rowData.isDeleted == true ?
                                HelperService.UndoDeleteButtonTable({{ requestUrl: '{entity.Name}/Restore', requestData: {{ {uniqueFieldParams} }}, pageTable: mainTable }}) :
                                HelperService.DeleteButtonTable({{requestUrl: '{entity.Name}/Delete', requestData: {{ {uniqueFieldParams} }}, pageTable: mainTable }});

                            DatatableManager.AppendRowButtons(td,
                            [
                                HelperService.UpdateButtonTable({{
                                    title: 'Update {entity.Name} Informations',
                                    formGetterUrl: '{entity.Name}/Update',
                                    requestData: {{
                                        {uniqueFieldParams}
                                    }},
                                    pageTable: mainTable
                                }}),
                                deleteHandleButton
                            ]);
                        }}
                    }}");
        }
        else
        {
            codeDatatableColumns.Append($@"
                    {{
                        data: null,
                        defaultContent: '',
                        searchable: false,
                        createdCell: function (td, cellData, rowData, row, col)
                        {{
                            DatatableManager.AppendRowButtons(td,
                            [
                                HelperService.UpdateButtonTable({{
                                    title: 'Update {entity.Name} Informations',
                                    formGetterUrl: '{entity.Name}/Update',
                                    requestData: {{
                                        {uniqueFieldParams}
                                    }},
                                    pageTable: mainTable
                                }}),
                                HelperService.DeleteButtonTable({{
                                    requestUrl: '{entity.Name}/Delete', 
                                    requestData: {{ {uniqueFieldParams} }}, 
                                    pageTable: mainTable 
                                }})
                            ]);
                        }}
                    }}");
        }
        #endregion

        return $@"
@using {_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}
@model {entity.Name}ViewModel
@{{
    ViewData[""Title""] = ""{entity.Name.Pluralize()}"";
}}

@await Html.PartialAsync(""Partials/Toolbar"", new Breadcrum
{{
    BreadcrumbItems = new List<BreadcrumbItem>
    {{
        new BreadcrumbItem {{ Title = ""Home"", Path = Url.Action(""Index"", ""Home"") }},
    }},
    PageName = ViewData[""Title""]?.ToString() ?? ""Page""
}})

<link href=""~/metronic/plugins/custom/datatables/datatables.bundle.css"" rel=""stylesheet"" />

<div class=""card card-flush"">
    <div class=""card-header align-items-center py-5 gap-2 gap-md-5"">
        <div class=""card-title"">
            <div class=""menu menu-sub menu-sub-dropdown mt-2 w-75 w-lg-500px"" data-kt-menu=""true"">
                <div class=""px-7 py-5"">
                    <div class=""fs-5 text-gray-900 fw-bold"">Filter Options</div>
                </div>
                <div class=""separator border-gray-200""></div>
                <div class=""px-7 py-5"">
                    {codeFilterForm.ToString()}
                    <div class=""d-flex justify-content-end"">
                        <button type=""reset"" class=""btn btn-sm btn-light btn-active-light-primary me-2"" data-kt-menu-dismiss=""true"">Reset</button>
                        <button type=""button"" onclick=""InitilazeTable(this)"" class=""btn btn-sm btn-primary"" data-kt-menu-dismiss=""true"">Apply</button>
                    </div>
                </div>
            </div>
            <button class=""btn btn-sm btn-flex btn-light-dark fw-bold"" data-kt-menu-trigger=""click"" data-kt-menu-placement=""bottom-start"">
                <i class=""ki-duotone ki-filter fs-6 text-muted me-1""><span class=""path1""></span><span class=""path2""></span></i>
                Filter
            </button>
        </div>
    </div>
    <div class=""card-body p-4 pt-0"">
        <table id=""main_table"" class=""table align-middle table-row-dashed fs-6 gy-5"">
            <thead>
                <tr class=""text-start text-gray-500 fw-bold fs-7 text-uppercase gs-0"">
                    {tableHeaderColumns.ToString()}
                </tr>
            </thead>
            <tbody class=""fw-semibold text-gray-600"">
            </tbody>
        </table>
    </div>
</div>

@section Scripts {{
    <script src=""~/metronic/plugins/custom/datatables/datatables.bundle.js""></script>

    <script>

        let mainTable;

        $(document).ready(function(){{
            InitilazeTable();
        }})

        function InitilazeTable(btn) {{
            mainTable = DatatableManager.Create({{
                serverSide: true,
                tableId: 'main_table',
                path: '{entity.Name}/DatatableServerSide',
                method: 'Post',
                buttonElement: btn,
                {codeDatatableRequestData}
                columns: [
                    {codeDatatableColumns.ToString()}
                ],
                customButtons:
                [  
                    {{
                        text: '<span class=""dynamic-content""><i class=""fa-solid fa-file-circle-plus me-2""></i>Add New {entity.Name}</span>',
                        className: 'btn btn-primary mx-2',
                        action: (e_btn) =>
                        {{
                            HelperService.InsertModal({{
                                title: 'Add New {entity.Name}',
                                formGetterUrl: '{entity.Name}/Create',
                                e_btn: e_btn.currentTarget,
                                pageTable: mainTable
                            }})
                        }}
                    }}
                ]
            }})
        }}
    </script>
}}
";
    }

    private string FormHtml(Entity entity, bool isUpdate)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType), enableTracking: false);

        int searchParam = isUpdate ? entity.UpdateDtoId ?? default : entity.CreateDtoId ?? default;
        Dto? dto = _dtoRepository.Get(f => f.Id == searchParam, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField).ThenInclude(x => x.FieldType));
        bool isThereDto = dto != default;

        StringBuilder codeFormInputs = new StringBuilder();

        if (isThereDto)
        {
            var selectableRelations = new Dictionary<string, string>();
            foreach (var dtoField in dto!.DtoFields.Where(f => f.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
            {
                Relation? relation = _relationRepository.Get(
                  filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                  include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                selectableRelations.Add(dtoField.SourceField.Name, relation.PrimaryField.Entity.Name);
            }

            foreach (var dtoField in dto!.DtoFields.Where(f => f.SourceField.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base))
            {
                int inptType = dtoField.SourceField.GetVariableGroup(selectableRelations);
                codeFormInputs.Append(HtmlInputGenerator.CreateFormInputHTML(dtoField.SourceField, inptType, isUpdate ? "UpdateModel" : "CreateModel"));
            }
        }
        else
        {
            var selectableRelations = new Dictionary<string, string>();
            foreach (var field in fieldList.Where(f => f.IsUnique == false))
            {
                Relation? relation = _relationRepository.Get(
                  filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)Enums.RelationTypeEnums.OneToMany,
                  include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity)
                );
                if (relation == null) continue;

                selectableRelations.Add(field.Name, relation.PrimaryField.Entity.Name);
            }

            foreach (var field in fieldList.Where(f => f.IsUnique == false))
            {
                int inptType = field.GetVariableGroup(selectableRelations);
                codeFormInputs.Append(HtmlInputGenerator.CreateFormInputHTML(field, inptType, isUpdate ? "UpdateModel" : "CreateModel"));
            }
        }

        return $@"
@using {_appSetting.WebUILayerProjectName}.Models.ViewModels.{entity.Name}
@model {entity.Name}{(isUpdate ? "Update" : "Create")}ViewModel
@{{
	Layout = null;
}}

<form asp-controller=""{entity.Name}"" asp-action=""{(isUpdate ? "Update" : "Create")}"" method=""post""> 
	<div class=""row row-gap-4 m-2"">
        {codeFormInputs.ToString()}
	</div>
</form>";
    }
    #endregion

    public string Copywwwroot()
    {
        if (string.IsNullOrEmpty(_appSetting.Path)) return "Warning: Not Found Destionation Path to Generation wwwroot files";

        var results = new List<string>();

        string basePath = AppContext.BaseDirectory;

        string sourcePath_assets = Path.GetFullPath(Path.Combine(basePath, @"CodeGenerators\NLayer\WebUI\wwwroot"));

        string destPath_assets = Path.GetFullPath(Path.Combine(_appSetting.SolutionPath, $@"{_appSetting.WebUILayerProjectName}\wwwroot"));

        results.Add(_fs.CopyDirectory(sourcePath_assets, destPath_assets));
        return "wwwroot generated";
    }
}







