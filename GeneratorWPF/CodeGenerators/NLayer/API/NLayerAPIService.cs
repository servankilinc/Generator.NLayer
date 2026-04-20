using GeneratorWPF.CodeGenerators.NLayer.Base;
using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GeneratorWPF.CodeGenerators.NLayer.API;

public class NLayerAPIService : NLayerGeneratorBase
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoRepository _dtoRepository;
    public NLayerAPIService(AppSetting appSetting) : base(appSetting)
    {
        _entityRepository = new();
        _fieldRepository = new();
        _dtoRepository = new();
    }

    public string CreateProject()
    {
        try
        {
            string layerPath = Path.Combine(_appSetting.SolutionPath, _appSetting.WebAPILayerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{_appSetting.WebAPILayerProjectName}.csproj");

            if (Directory.Exists(layerPath) && File.Exists(csprojPath))
                return "INFO: WebAPI layer project already exists.";

            RunCommand(_appSetting.SolutionPath, "dotnet", $"new webapi -n {_appSetting.WebAPILayerProjectName}");


            bool isSlnx = File.Exists(Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.slnx"));
      
            RunCommand(_appSetting.SolutionPath, "dotnet", $"sln {_appSetting.SolutionName}.{(isSlnx ? "slnx" : "sln")} add {_appSetting.WebAPILayerProjectName}/{_appSetting.WebAPILayerProjectName}.csproj");
            RemoveFile(layerPath, "Program.cs");
            RemoveFile(layerPath, "appsettings.json");

            RemoveFile(layerPath, "Controllers/WeatherForecastController.cs");
            RemoveFile(layerPath, "WeatherForecast.cs");

            RunCommand(layerPath, "dotnet", $"add reference ../{_appSetting.BusinessLayerProjectName}/{_appSetting.BusinessLayerProjectName}.csproj");

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
            if (dtos.Any(f => f.CrudTypeId != (byte)CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands");
            if (dtos.Any(f => f.CrudTypeId == (byte)CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Queries");


            var code_controller = CompilationUnit(
                usings: [
                    "Microsoft.AspNetCore.Authorization",
                    "Microsoft.AspNetCore.Mvc",
                    $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",
                    $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    $"{_appSetting.WebAPILayerProjectName}.Controllers.Base",
                    ..dtoUsings
                ],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.WebAPILayerProjectName}.Controllers",
                    members: [
                        ClassDeclaration(
                            name: $"{entity.Name}Controller",
                            modifiers: [SyntaxKind.PublicKeyword],
                            baseTypes: [SyntaxFactory.ParseTypeName("BaseController")],
                            members: [
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], $"I{entity.Name}Service", $"_{entity.Name.ToCamelCase()}Service"),
                                ConstructorDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword],
                                    name: $"{entity.Name}Controller",
                                    parameters: [
                                        ParameterDeclaration($"ILogger<{entity.Name}Controller>", "logger"),
                                        ParameterDeclaration($"I{entity.Name}Service", $"{entity.Name.ToCamelCase()}Service")
                                    ],
                                    baseArgs: ["logger"],
                                    statements: [StatementExpression($"_{entity.Name.ToCamelCase()}Service", $"{entity.Name.ToCamelCase()}Service")]
                                ),
                                ..GenerateControllerMethods(entity, dtos)
                            ]
                        )
                    ]
                )
            );

            results.Add(AddFile(folderPath, $"{entity.Name}Controller.cs", code_controller.ToFullString()));
        }

        return string.Join("\n", results);
    }

    private List<MethodDeclarationSyntax> GenerateControllerMethods(Entity entity, List<Dto> dtos)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).ToList();
        var uniqueFieldParameters = uniqueFields.Select(f => ParameterDeclaration(f.GetMapedTypeName(), f.Name.ToCamelCase(), true)).ToList();

        string methodUniqueArgs = string.Join(", ", uniqueFields.Select(f => $"{f.Name.ToCamelCase()}: {f.Name.ToCamelCase()}"));

        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        #region GET
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpGet", $"{{{entity.GetConstraintRule()}}}")],
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

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            string reqKind = dto.Id == entity.BasicResponseDtoId ? "base" :
                    dto.Id == entity.DetailResponseDtoId ? "detail" : dto.Name.ToCamelCase();

            string methodName = dto.ServiceGetMethodName(entity);

            methods.Add(MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpGet", $"{{{entity.GetConstraintRule()}}}/{reqKind}")],
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
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "list")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetList",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration("DynamicRequest", "request", false)
            ],
            body: @$"
                var result = await {serviceName}.GetListAsync(request);
                return ToAction(result);
            "
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            string reqKind = dto.Id == entity.BasicResponseDtoId ? "base" :
                    dto.Id == entity.DetailResponseDtoId ? "detail" : dto.Name.ToCamelCase();

            string methodName = dto.ServiceGetListMethodName(entity);

            methods.Add(MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpPost", $"list/{reqKind}")],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.PresentationLayerListMethodName(entity),
                returnType: "Task<IActionResult>",
                parameters: [
                    ParameterDeclaration("DynamicRequest", "request", false),
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
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Create",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration(createDto?.Name ?? entity.Name, "request", true)
            ],
            body: $@"
                var result = await {serviceName}.CreateAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        #region UPDATE
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);

        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpGet", "update")],
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

        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPut")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Update",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration(createDto?.Name ?? entity.Name, "request", true)
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
            methods.Add(MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpPost", "delete")],
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "Delete",
                returnType: "Task<IActionResult>",
                parameters:
                [
                    ParameterDeclaration(deleteDto.Name, "request", true)
                ],
                body: $@"
                    await {serviceName}.DeleteAsync(request);
                    return Ok();
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpDelete", $"{{{entity.GetConstraintRule()}}}")],
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
            methods.Add(MethodDeclaration(
                attributes: [GenerateHttpAttribute("HttpGet", $"{{{entity.GetConstraintRule()}}}/restore")],
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
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "pagination")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "Pagination",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration("DynamicPaginationRequest", "request", true)
            ],
            body: $@"
                var result = await {serviceName}.PaginationAsync(request);
                return ToAction(result);
            "
        ));
        #endregion

        #region DATATABLE 
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "datatable/client")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableClientSide",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true)
            ],
            body: $@"
                var result = await {serviceName}.DatatableClientSideAsync(request);
                return ToAction(result);
            "
        ));
        methods.Add(MethodDeclaration(
            attributes: [GenerateHttpAttribute("HttpPost", "datatable/server")],
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableServerSide",
            returnType: "Task<IActionResult>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true)
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
                    options.User.AllowedUserNameCharacters = ""abcçdefgğhiıjklmnoöpqrsştuüvwxyzABCÇDEFGĞHIİJKLMNOÖPQRSŞTUÜVWXYZ0123456789-._@+/*|!,;:()&#?[] "";
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
