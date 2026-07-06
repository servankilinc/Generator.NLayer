using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Core;
using Generator.Domain.Core.Entities;
using Generator.Domain.CodeGenerators.Helpers;

namespace Generator.Domain.CodeGenerators.NLayer.API;

public class NLayerAPIService : IGenerationStep
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoRepository _dtoRepository;

    private readonly FileSystemService _fs;
    private readonly RoslynSyntaxHelper _roslyn;
    private readonly DotnetCliService _cli;
    private readonly TemplateRenderer _templateRenderer;

    private AppSetting _appSetting = null!;

    public string Name => "API Layer";
    public int Order => 5;
    public int ProgressWeight => 15;

    public NLayerAPIService(
        EntityRepository entityRepository,
        FieldRepository fieldRepository,
        DtoRepository dtoRepository,
        FileSystemService fs,
        RoslynSyntaxHelper roslyn,
        DotnetCliService cli,
        TemplateRenderer templateRenderer)
    {
        _entityRepository = entityRepository;
        _fieldRepository = fieldRepository;
        _dtoRepository = dtoRepository;
        _fs = fs;
        _roslyn = roslyn;
        _cli = cli;
        _templateRenderer = templateRenderer;
        _templateRenderer = templateRenderer;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            _appSetting = appSetting;
            log(CreateProject());
            log(_cli.AddPackage(_appSetting, "Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(_cli.AddPackage(_appSetting, "Microsoft.AspNetCore.OpenApi --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(_cli.AddPackage(_appSetting, "Microsoft.EntityFrameworkCore.Design --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(_cli.AddPackage(_appSetting, "Scalar.AspNetCore --version 2.14.1", _appSetting.WebAPILayerProjectName));
            log(_cli.Restore(_appSetting, _appSetting.WebAPILayerProjectName));
            log(_templateRenderer.GenerateStaticFiles(_appSetting, "API", _appSetting.WebAPILayerProjectName));
            log(GenerateControllers());
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
            string layerPath = Path.Combine(_appSetting.SolutionPath, _appSetting.WebAPILayerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{_appSetting.WebAPILayerProjectName}.csproj");

            if (Directory.Exists(layerPath) && File.Exists(csprojPath))
                return "INFO: WebAPI layer project already exists.";

            _cli.RunCommand(_appSetting.SolutionPath, "dotnet", $"new webapi -n {_appSetting.WebAPILayerProjectName}");


            bool isSlnx = File.Exists(Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.slnx"));
      
            _cli.RunCommand(_appSetting.SolutionPath, "dotnet", $"sln {_appSetting.SolutionName}.{(isSlnx ? "slnx" : "sln")} add {_appSetting.WebAPILayerProjectName}/{_appSetting.WebAPILayerProjectName}.csproj");
            _fs.RemoveFile(layerPath, "Program.cs");
            _fs.RemoveFile(layerPath, "appsettings.json");

            _fs.RemoveFile(layerPath, "Controllers/WeatherForecastController.cs");
            _fs.RemoveFile(layerPath, "WeatherForecast.cs");

            _cli.RunCommand(layerPath, "dotnet", $"add reference ../{_appSetting.BusinessLayerProjectName}/{_appSetting.BusinessLayerProjectName}.csproj");

            return "OK: WebAPI Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the WebAPI project. \n\t Details:{ex.Message}");
        }
    }


    #region Controllers
    public string GenerateControllers()
    {
        var results = new List<string>();

        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.WebAPILayerProjectName, "Controllers");

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(x => x.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields));

            List<string> dtoUsings = new();
            if (dtos.Any(f => f.CrudTypeId != (byte)Enums.CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands");
            if (dtos.Any(f => f.CrudTypeId == (byte)Enums.CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Queries");


            var code_controller = _roslyn.CompilationUnit(
                usings: [
                    "Microsoft.AspNetCore.Authorization",
                    "Microsoft.AspNetCore.Mvc",
                    $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",
                    $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    $"{_appSetting.WebAPILayerProjectName}.Controllers.Base",
                    ..dtoUsings
                ],
                nspace: _roslyn.NamespaceDeclaration(
                    value: $"{_appSetting.WebAPILayerProjectName}.Controllers",
                    members: [
                        _roslyn.ClassDeclaration(
                            name: $"{entity.Name}Controller",
                            modifiers: [SyntaxKind.PublicKeyword],
                            baseTypes: [SyntaxFactory.ParseTypeName("BaseController")],
                            members: [
                                _roslyn.FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], $"I{entity.Name}Service", $"_{entity.Name.ToCamelCase()}Service"),
                                _roslyn.ConstructorDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword],
                                    name: $"{entity.Name}Controller",
                                    parameters: [
                                        _roslyn.ParameterDeclaration($"ILogger<{entity.Name}Controller>", "logger"),
                                        _roslyn.ParameterDeclaration($"I{entity.Name}Service", $"{entity.Name.ToCamelCase()}Service")
                                    ],
                                    baseArgs: ["logger"],
                                    statements: [_roslyn.StatementExpression($"_{entity.Name.ToCamelCase()}Service", $"{entity.Name.ToCamelCase()}Service")]
                                ),
                                ..GenerateControllerMethods(entity, dtos)
                            ]
                        )
                    ]
                )
            );

            results.Add(_fs.AddFile(folderPath, $"{entity.Name}Controller.cs", code_controller.ToFullString()));
        }

        return string.Join("\n", results);
    }

    private List<MethodDeclarationSyntax> GenerateControllerMethods(Entity entity, List<Dto> dtos)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).ToList();
        var uniqueFieldParameters = uniqueFields.Select(f => _roslyn.ParameterDeclaration(f.GetMapedTypeName(), f.Name.ToCamelCase(), true)).ToList();

        string methodUniqueArgs = string.Join(", ", uniqueFields.Select(f => $"{f.Name.ToCamelCase()}: {f.Name.ToCamelCase()}"));

        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        #region GET
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpGet", $"{EntityCodeHelper.GetConstraintRule(entity)}")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Get",
            returnType: "Task<IActionResult>",
            parameters: [
                ..uniqueFieldParameters
            ],
            body: @$"
                var result = await {serviceName}.GetAsync({methodUniqueArgs});
                return ToAction(result);
            "
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)Enums.CrudTypeEnums.Read))
        {
            string reqKind = dto.Id == entity.BasicResponseDtoId ? "base" :
                    dto.Id == entity.DetailResponseDtoId ? "detail" : dto.Name.ToCamelCase();

            string methodName = dto.ServiceGetMethodName(entity);

            methods.Add(_roslyn.MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpGet", $"{EntityCodeHelper.GetConstraintRule(entity)}/{reqKind}")],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.PresentationLayerGetMethodName(entity),
                returnType: "Task<IActionResult>",
                parameters: [
                    ..uniqueFieldParameters
                ],
                body: @$"
                    var result = await {serviceName}.{methodName}({methodUniqueArgs});
                    return ToAction(result);
                "
            ));
        }
        #endregion

        #region GET LIST
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "list")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetList",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration("DynamicRequest", "request", false)
            ],
            body: @$"
                var result = await {serviceName}.GetListAsync(request);
                return ToAction(result);
            "
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)Enums.CrudTypeEnums.Read))
        {
            string reqKind = dto.Id == entity.BasicResponseDtoId ? "base" :
                    dto.Id == entity.DetailResponseDtoId ? "detail" : dto.Name.ToCamelCase();

            string methodName = dto.ServiceGetListMethodName(entity);

            methods.Add(_roslyn.MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpPost", $"list/{reqKind}")],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.PresentationLayerListMethodName(entity),
                returnType: "Task<IActionResult>",
                parameters: [
                    _roslyn.ParameterDeclaration("DynamicRequest", "request", false),
                ],
                body: $@"
                    var result = await {serviceName}.{methodName}(request);
                    return ToAction(result);
                "
            ));
        }
        #endregion

        #region CREATE
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost")],
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
            attributes: [GenerateHttpAttribute("HttpGet", $"{EntityCodeHelper.GetConstraintRule(entity)}/update")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Update",
            returnType: "Task<IActionResult>",
            parameters: [
                ..uniqueFieldParameters
            ],
            body: $@"
                var result = await {serviceName}.{(updateDto != null ? "GetUpdateModelAsync" : "GetAsync")}({methodUniqueArgs});
                return ToAction(result);
            "
        ));

        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPut")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Update",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration(updateDto?.Name ?? entity.Name, "request", true)
            ],
            body: $@"
                var result = await {serviceName}.UpdateAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        #region DELETE
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        if (deleteDto != null)
        {
            methods.Add(_roslyn.MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpPost", "delete")],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "Delete",
                returnType: "Task<IActionResult>",
                parameters:
                [
                    _roslyn.ParameterDeclaration(deleteDto.Name, "request", true)
                ],
                body: $@"
                    await {serviceName}.DeleteAsync(request);
                    return Ok();
                "
            ));
        }
        else
        {
            methods.Add(_roslyn.MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpDelete", $"{EntityCodeHelper.GetConstraintRule(entity)}")],
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
                attributes: [GenerateHttpAttribute("HttpGet", $"{EntityCodeHelper.GetConstraintRule(entity)}/restore")],
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

        #region PAGINATION 
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "pagination")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Pagination",
            returnType: "Task<IActionResult>",
            parameters: [
                _roslyn.ParameterDeclaration("DynamicPaginationRequest", "request", true)
            ],
            body: $@"
                var result = await {serviceName}.PaginationAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        #region DATATABLE 
        methods.Add(_roslyn.MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "datatable/client")],
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
            attributes: [GenerateHttpAttribute("HttpPost", "datatable/server")],
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

    private static AttributeSyntax GenerateHttpAttribute(string route, string? template = null)
    {
        if (template == null)
        {
            return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(route));
        }
        return
        SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(route),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(template)
                        )
                    )
                )
            )
        );
    }
    #endregion


    public string GetIdentityRegistrationCode()
    {
        if (!_appSetting.IsThereIdentity)
            return string.Empty;

        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string IdentityUserType = identityTypeConfigs.IdentityUserType;
        string IdentityRoleType = identityTypeConfigs.IdentityRoleType;

        return @$"
            #region ------- IDENTITY -------
            builder.Services
                .AddIdentity<{IdentityUserType}, {IdentityRoleType}>(options =>
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
                    options.User.AllowedUserNameCharacters = ""abcÃƒÂ§defgÃ„Å¸hiÃ„Â±jklmnoÃƒÂ¶pqrsÃ…Å¸tuÃƒÂ¼vwxyzABCÃƒâ€¡DEFGÃ„ÂHIÃ„Â°JKLMNOÃƒâ€“PQRSÃ…ÂTUÃƒÅ“VWXYZ0123456789-._@+/*|!,;:()&#?[] "";
                }})
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthorization();
            #endregion


            #region ------- JWT Implementation -------
            TokenSettings tokenSettings = builder.Configuration.GetSection(""TokenSettings"").Get<TokenSettings>()!;
            builder.Services.AddSingleton(tokenSettings);

            builder.Services
                .AddAuthentication(options =>
                {{
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }})
                .AddJwtBearer(options =>
                {{
                    options.TokenValidationParameters = new TokenValidationParameters
                    {{
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidIssuer = tokenSettings.Issuer,
                        ValidAudience = tokenSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSettings.SecurityKey))
                    }};
                }});
            #endregion
        ";
    }
}






