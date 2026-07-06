using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Core;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.CodeGenerators.NLayer.Model;

public class NLayerModelGenerator : IGenerationStep
{
    private readonly EntityRepository _entityRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;
    private readonly DtoFieldRepository _dtoFieldRepository;
    
    private readonly FileSystemService _fs;
    private readonly RoslynSyntaxHelper _roslyn;
    private readonly ValidationRuleGenerator _validation;
    private readonly DotnetCliService _cli;
    private readonly TemplateRenderer _templateRenderer;

    public string Name => "Model Layer";
    public int Order => 2;
    public int ProgressWeight => 20;

    public NLayerModelGenerator(
        EntityRepository entityRepository,
        DtoRepository dtoRepository,
        FieldRepository fieldRepository,
        RelationRepository relationRepository,
        DtoFieldRepository dtoFieldRepository,
        FileSystemService fs,
        RoslynSyntaxHelper roslyn,
        ValidationRuleGenerator validation,
        DotnetCliService cli,
        TemplateRenderer templateRenderer)
    {
        _entityRepository = entityRepository;
        _dtoRepository = dtoRepository;
        _fieldRepository = fieldRepository;
        _relationRepository = relationRepository;
        _dtoFieldRepository = dtoFieldRepository;
        _fs = fs;
        _roslyn = roslyn;
        _validation = validation;
        _cli = cli;
        _templateRenderer = templateRenderer;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            log(_cli.CreateClassLibraryProject(appSetting, appSetting.ModelLayerProjectName, new[] { $"../{appSetting.CoreLayerProjectName}/{appSetting.CoreLayerProjectName}.csproj" }));
            log(_templateRenderer.GenerateStaticFiles(appSetting, "Model", appSetting.ModelLayerProjectName));
            log(GenerateAuthModels(appSetting));
            log(GenerateEntities(appSetting));
            log(GenerateDtos(appSetting));
            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    #region Auth-Models
    private string GenerateAuthModels(AppSetting appSetting)
    {
        var results = new List<string>();
        var uniqueFields = _fieldRepository.GetAll(f => f.EntityId == appSetting.UserEntityId && f.IsUnique);

        #region 1. Login Models
        var loginRequest = _roslyn.CompilationUnit(
            usings: [
                "FluentValidation",
                $"{appSetting.CoreLayerProjectName}.Utils.CriticalData"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.Login",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginRequest",
                        members: [
                            _roslyn.PropertyDeclaration("string", "Email", false),
                            _roslyn.PropertyDeclaration("string", "UserName", false),
                            _roslyn.PropertyDeclaration("string", "Password", true, attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CriticalData"))]),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", false),
                            _roslyn.PropertyDeclaration("string", "ClientType", true)
                        ]
                    ),
                    _roslyn.ValidatorClassDeclaration(
                        modelName: "LoginRequest",
                        ruleList: [
                            "RuleFor(b => b).Must(b => !string.IsNullOrWhiteSpace(b.Email) || !string.IsNullOrWhiteSpace(b.UserName)).WithMessage(\"Either Email or UserName must be provided.\");",
                            "RuleFor(b => b.UserName).MinimumLength(6).When(b => !string.IsNullOrWhiteSpace(b.UserName));",
                            "RuleFor(b => b.Email).EmailAddress().When(b => !string.IsNullOrWhiteSpace(b.Email));",
                            "RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.ClientType).NotNull().NotEmpty();"
                        ]
                    )
                ]
            )
        );
        var loginResponse = _roslyn.CompilationUnit(
            usings: [$"{appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.Login",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginResponse",
                        members: [
                            _roslyn.PropertyDeclaration("IList<string>", "Roles", false),
                            _roslyn.PropertyDeclaration("AccessToken", "AccessToken", true),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", true)
                        ]
                    ),
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("LoginResponse")],
                        members: [_roslyn.PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathLogin = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Auth", "Login");
        results.Add(_fs.AddFile(folderPathLogin, "LoginRequest.cs", loginRequest.ToFullString()));
        results.Add(_fs.AddFile(folderPathLogin, "LoginResponse.cs", loginResponse.ToFullString()));
        #endregion

        #region 2. Refresh Auth Models
        var refreshAuthRequest = _roslyn.CompilationUnit(
            usings: ["FluentValidation"],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.Refresh",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthRequest",
                        members: [
                            _roslyn.PropertyDeclaration("string", "RefreshToken", false),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", true),
                            _roslyn.PropertyDeclaration(uniqueFields.FirstOrDefault()?.GetMapedTypeName() ?? "Guid", "UserId", true)
                        ]
                    ),
                    _roslyn.ValidatorClassDeclaration(
                        modelName: "RefreshAuthRequest",
                        ruleList: [
                            "RuleFor(b => b.UserId).NotNull().NotEmpty();",
                            "RuleFor(b => b.DeviceId).NotNull().NotEqual(Guid.Empty).NotEmpty();"
                        ]
                    )
                ]
            )
        );

        var refreshAuthResponse = _roslyn.CompilationUnit(
            usings: [$"{appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.Refresh",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthResponse",
                        members: [
                            _roslyn.PropertyDeclaration("IList<string>", "Roles", false),
                            _roslyn.PropertyDeclaration("AccessToken", "AccessToken", true),
                        ]
                    ),
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("RefreshAuthResponse")],
                        members: [_roslyn.PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathRefreshAuth = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Auth", "RefreshAuth");
        results.Add(_fs.AddFile(folderPathRefreshAuth, "RefreshAuthRequest.cs", refreshAuthRequest.ToFullString()));
        results.Add(_fs.AddFile(folderPathRefreshAuth, "RefreshAuthResponse.cs", refreshAuthResponse.ToFullString()));
        #endregion

        #region 3. Signup Models
        var code_SignUpRequest = _roslyn.CompilationUnit(
            usings: [
                "FluentValidation",
                $"{appSetting.CoreLayerProjectName}.Utils.CriticalData"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.SignUp",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpRequest",
                        members: [
                            _roslyn.PropertyDeclaration("string", "Email", true),
                            _roslyn.PropertyDeclaration("string", "UserName", true),
                            _roslyn.PropertyDeclaration("string", "Password", true, attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CriticalData"))]),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", false),
                            _roslyn.PropertyDeclaration("string", "ClientType", true)
                        ]
                    ),
                    _roslyn.ValidatorClassDeclaration(
                        modelName: "SignUpRequest",
                        ruleList:  [
                            "RuleFor(b => b.Email).NotNull().NotEmpty().EmailAddress();",
                            "RuleFor(b => b.UserName).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.ClientType).NotNull().NotEmpty();"
                        ]
                    )
                ]
            )
        );

        var code_SignUpResponse = _roslyn.CompilationUnit(
            usings: [$"{appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Auth.SignUp",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpResponse",
                        members: [
                            _roslyn.PropertyDeclaration("IList<string>", "Roles", false),
                            _roslyn.PropertyDeclaration("AccessToken", "AccessToken", true),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", true)
                        ]
                    ),
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("SignUpResponse")],
                        members: [_roslyn.PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathSignup = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Auth", "SignUp");
        results.Add(_fs.AddFile(folderPathSignup, "SignUpRequest.cs", code_SignUpRequest.ToFullString()));
        results.Add(_fs.AddFile(folderPathSignup, "SignUpResponse.cs", code_SignUpResponse.ToFullString()));
        #endregion

        return string.Join("\n", results);
    }
    #endregion

    #region Entities
    private string GenerateEntities(AppSetting appSetting)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            string code = HandleGenerateEntity(entity, appSetting);

            string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Entities");
            results.Add(_fs.AddFile(folderPath, $"{entity.Name}.cs", code));
        }

        #region RefreshToken
        if (appSetting.IsThereIdentity)
        {
            var identityTypeConfigs = appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);

            string code_refreshToken = _roslyn.CompilationUnit(
                usings: [$"{appSetting.CoreLayerProjectName}.Model"],
                nspace: _roslyn.NamespaceDeclaration(
                    value: $"{appSetting.ModelLayerProjectName}.Entities",
                    members: [
                        _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshToken",
                        baseTypes: [SyntaxFactory.IdentifierName("IEntity")],
                        members: [
                            _roslyn.PropertyDeclaration("Guid", "Id", true),
                            _roslyn.PropertyDeclaration(identityTypeConfigs.IdentityKeyType, "UserId", true),
                            _roslyn.PropertyDeclaration("Guid", "DeviceId", true),
                            _roslyn.PropertyDeclaration("string", "IpAddress", false),
                            _roslyn.PropertyDeclaration("string", "ClientType", false),
                            _roslyn.PropertyDeclaration("string", "Token", true),
                            _roslyn.PropertyDeclaration("DateTime", "ExpirationUtc", true),
                            _roslyn.PropertyDeclaration("DateTime", "CreateDateUtc", true),
                            _roslyn.PropertyDeclaration("int", "TTL", true),
                            _roslyn.PropertyDeclaration("bool", "IsRevoked", true),
                            _roslyn.PropertyDeclaration($"virtual {identityTypeConfigs.IdentityUserType}?", "User", false)
                        ])
                    ]
                )
            ).ToFullString();

            string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Entities");
            results.Add(_fs.AddFile(folderPath, "RefreshToken.cs", code_refreshToken));
        }
        #endregion

        return string.Join("\n", results);
    }

    private string HandleGenerateEntity(Entity entity, AppSetting appSetting)
    {
        // 1) Implemantation List
        List<string> interfaces = new();
        if (appSetting.IsThereUser && appSetting.UserEntityId == entity.Id)
            interfaces.Add($"IdentityUser<{entity.Fields.FirstOrDefault(f => f.IsUnique)?.GetMapedTypeName()}>");
        if (appSetting.IsThereRole && appSetting.RoleEntityId == entity.Id)
            interfaces.Add($"IdentityRole<{entity.Fields.FirstOrDefault(f => f.IsUnique)?.GetMapedTypeName()}>");

        interfaces.Add(Statics.IEntity);
        if (entity.SoftDeletable) interfaces.Add(Statics.ISoftDeletableEntity);
        if (entity.Archivable) interfaces.Add(Statics.IArchivableEntity);
        if (entity.Auditable) interfaces.Add(Statics.IAuditableEntity);

        // 2) Property List
        List<PropertyDeclarationSyntax> properties = new();

        var baseTypeFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));
        foreach (var field in baseTypeFields.Where(f => f.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base))
        {
            string fieldTypeName = field.GetMapedTypeName();
            if (field.IsList) fieldTypeName = $"List<{fieldTypeName}>";

            properties.Add(_roslyn.PropertyDeclaration(fieldTypeName, field.Name, field.IsRequired));
        }

        // 3) Implemented Ä°nterface List
        if (entity.Auditable)
        {
            properties.Add(_roslyn.PropertyDeclaration("string", "CreatedBy", false));
            properties.Add(_roslyn.PropertyDeclaration("string", "UpdatedBy", false));
            properties.Add(_roslyn.PropertyDeclaration("DateTime", "CreateDateUtc", false));
            properties.Add(_roslyn.PropertyDeclaration("DateTime", "UpdateDateUtc", false));
        }
        if (entity.SoftDeletable)
        {
            properties.Add(_roslyn.PropertyDeclaration("string", "DeletedBy", false));
            properties.Add(_roslyn.PropertyDeclaration("bool", "IsDeleted"));
            properties.Add(_roslyn.PropertyDeclaration("DateTime", "DeletedDateUtc", false));
        }

        // 4) Virtual Propert List
        HandleVirtualProps(ref properties, entity.Id, appSetting);


        // 5) Usings
        List<string> usings = new(){
            $"{appSetting.CoreLayerProjectName}.Model"
        };

        if (appSetting.UserEntityId == entity.Id || appSetting.RoleEntityId == entity.Id)
            usings.Add("Microsoft.AspNetCore.Identity");

        return _roslyn.CompilationUnit(
            usings: [.. usings],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Entities",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: entity.Name,
                        baseTypes: [.. interfaces.Select(i => SyntaxFactory.ParseTypeName(i))],
                        members: [..properties]
                    )
                ]
            )
        ).ToFullString();
    }

    private void HandleVirtualProps(ref List<PropertyDeclarationSyntax> propertyList, int entityId, AppSetting appSetting)
    {
        var relationsOnPrimary = _relationRepository.GetRelationsOnPrimary(entityId);
        var relationsOnForeign = _relationRepository.GetRelationsOnForeign(entityId);

        foreach (var relation in relationsOnPrimary)
        {
            if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToOne)
            {
                propertyList.Add(_roslyn.PropertyDeclaration($"{relation.ForeignField.Entity.Name}?", relation.PrimaryEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }
        foreach (var relation in relationsOnForeign)
        {
            if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToOne)
            {
                propertyList.Add(_roslyn.PropertyDeclaration($"{relation.PrimaryField.Entity.Name}?", relation.ForeignEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
            else if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToMany)
            {
                propertyList.Add(_roslyn.PropertyDeclaration($"{relation.PrimaryField.Entity.Name}?", relation.ForeignEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }

        foreach (var relation in relationsOnPrimary)
        {
            if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToMany)
            {
                propertyList.Add(_roslyn.PropertyDeclaration($"ICollection<{relation.ForeignField.Entity.Name}>?", relation.PrimaryEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }

        if (entityId == appSetting.UserEntityId)
        {
            propertyList.Add(_roslyn.PropertyDeclaration("ICollection<RefreshToken>?", "RefreshTokens", modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
        }
    }
    #endregion

    #region Dtos
    private string GenerateDtos(AppSetting appSetting)
    {
        var results = new List<string>();

        var dtos = _dtoRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.RelatedEntity).ThenInclude(x => x.Fields));

        foreach (var dto in dtos)
        {
            string code = HandleGenerateDto(dto, appSetting);

            string commandOrQuery = dto.CrudTypeId == (int)Enums.CrudTypeEnums.Read ? "Queries" : "Commands";
            string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.ModelLayerProjectName, "Dtos", dto.RelatedEntity.Name, commandOrQuery);

            results.Add(_fs.AddFile(folderPath, $"{dto.Name}.cs", code));
        }
        return string.Join("\n", results);
    }

    private string HandleGenerateDto(Dto dto, AppSetting appSetting)
    {
        var dtoFieldList = _dtoFieldRepository.GetAll(
            filter: f => f.DtoId == dto.Id,
            include: i => i
                .Include(x => x.SourceField).ThenInclude(x => x.FieldType)
                .Include(x => x.SourceField).ThenInclude(x => x.Entity).ThenInclude(x => x.Dtos)
                .Include(x => x.Validations!).ThenInclude(x => x.ValidatorType)
                .Include(x => x.Validations!).ThenInclude(x => x.ValidationParams)
            );
        bool isExistValidation = dtoFieldList.Any(f => f.Validations != null && f.Validations.Any());

        #region 1) Property List
        List<PropertyDeclarationSyntax> properties = new();

        bool isReportDto = dto.RelatedEntity.ReportDtoId == dto.Id;
        if (isReportDto)
        {
            List<Field> uniqueFields = dto.RelatedEntity.Fields.Where(f => f.IsUnique).ToList();
            foreach (var unqField in uniqueFields)
            {
                // if there is no unique field with same name in dto
                if (dtoFieldList.Any(f => f.SourceFieldId == unqField.Id && f.Name.Trim() == unqField.Name.Trim()) == false)
                    properties.Add(_roslyn.PropertyDeclaration(unqField.GetMapedTypeName(), unqField.Name, true));
            }
        }

        foreach (var dtoField in dtoFieldList)
        {
            string fieldTypeName = dtoField.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base ?
                dtoField.SourceField.GetMapedTypeName() : dtoField.SourceField.FieldType.Name;

            if (dtoField.SourceField.IsList)
                fieldTypeName = $"List<{fieldTypeName}>";
            //if (!dtoField.SourceField.IsRequired)
            //    fieldTypeName = $"{fieldTypeName}?";
            if (dtoField.IsList)
                fieldTypeName = $"List<{fieldTypeName}>";
            if (!dtoField.IsRequired)
                fieldTypeName = $"{fieldTypeName}?";

            properties.Add(_roslyn.PropertyDeclaration(fieldTypeName, dtoField.Name, dtoField.IsRequired));
        }
        if (isReportDto)
        {
            if (dto.RelatedEntity.Auditable)
            {
                properties.Add(_roslyn.PropertyDeclaration("string", "CreatedBy", false));
                properties.Add(_roslyn.PropertyDeclaration("string", "UpdatedBy", false));
                properties.Add(_roslyn.PropertyDeclaration("DateTime", "CreateDateUtc", false));
                properties.Add(_roslyn.PropertyDeclaration("DateTime", "UpdateDateUtc", false));
            }
            if (dto.RelatedEntity.SoftDeletable)
            {
                properties.Add(_roslyn.PropertyDeclaration("string", "DeletedBy", false));
                properties.Add(_roslyn.PropertyDeclaration("bool", "IsDeleted", true));
                properties.Add(_roslyn.PropertyDeclaration("DateTime", "DeletedDateUtc", false));
            }
        }
        #endregion

        #region 2) Usings
        List<string> usings = new(){
            $"{appSetting.CoreLayerProjectName}.Model"
        };
        if (isExistValidation)
            usings.Add("FluentValidation");
        if (dtoFieldList.Any(f => f.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Entity))
            usings.Add($"{appSetting.ModelLayerProjectName}.Entities");

        if (dtoFieldList.Any(f => f.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Dto))
        {
            List<int> addedSourceEntites = new() { dto.RelatedEntityId };
            foreach (var dtoField in dtoFieldList.Where(f => f.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Dto))
            {
                if (addedSourceEntites.Any(f => f == dtoField.SourceField.EntityId))
                    continue;

                addedSourceEntites.Add(dtoField.SourceField.EntityId);
                if (dtoField.SourceField.Entity.Dtos?.Any(f => f.CrudTypeId != (int)Enums.CrudTypeEnums.Read) == true)
                    usings.Add($"{appSetting.ModelLayerProjectName}.Dtos.{dtoField.SourceField.Entity.Name}.Commands");
                if (dtoField.SourceField.Entity.Dtos?.Any(f => f.CrudTypeId == (int)Enums.CrudTypeEnums.Read) == true)
                    usings.Add($"{appSetting.ModelLayerProjectName}.Dtos.{dtoField.SourceField.Entity.Name}.Queries");
            }
        }
        #endregion

        List<ClassDeclarationSyntax> classes = new()
        {
            _roslyn.ClassDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: dto.Name,
                baseTypes: [SyntaxFactory.ParseTypeName("IDto")],
                members: [..properties]
            )
        };

        if (isExistValidation)
        {
            var rules = new List<string>();
            foreach (var dtoField in dtoFieldList)
            {
                if (dtoField.Validations == null || !dtoField.Validations.Any())
                    continue;

                foreach (var validation in dtoField.Validations)
                {
                    string rule = _validation.ValidationRule(validation, dtoField.Name, validation.ErrorMessage);
                    if (string.IsNullOrWhiteSpace(rule)) continue;
                    rules.Add(rule);
                }
            }
            classes.Add(_roslyn.ValidatorClassDeclaration(dto.Name, [.. rules]));
        }

        return _roslyn.CompilationUnit(
            usings: [.. usings],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.ModelLayerProjectName}.Dtos.{dto.RelatedEntity.Name}.{(dto.CrudTypeId == (int)Enums.CrudTypeEnums.Read ? "Queries" : "Commands")}",
                members: [.. classes]
            )
        ).ToFullString();
    }
    #endregion
}





