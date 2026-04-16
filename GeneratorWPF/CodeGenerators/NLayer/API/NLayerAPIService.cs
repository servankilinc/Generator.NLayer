using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.API;

public class NLayerAPIService
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly AppSetting _appSetting;
    public NLayerAPIService(AppSetting appSetting)
    {
        _appSetting = appSetting;
        _entityRepository = new();
        _fieldRepository = new();
        _dtoRepository = new();
    }

    public string CreateProject(string path, string solutionName)
    {
        try
        {
            string projectPath = Path.Combine(path, "WebAPI");
            string csprojPath = Path.Combine(projectPath, "WebAPI.csproj");

            if (Directory.Exists(projectPath) && File.Exists(csprojPath))
                return "INFO: WebAPI layer project already exists.";

            RunCommand(path, "dotnet", $"new webapi -n WebAPI");
            RunCommand(path, "dotnet", $"sln {solutionName}.sln add WebAPI/WebAPI.csproj");
            RunCommand(projectPath, "dotnet", $"add reference ../Business/Business.csproj");

            RemoveFile(projectPath, "Program.cs");
            RemoveFile(projectPath, "appsettings.json");

            return "OK: WebAPI Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the WebAPI project. \n\t Details:{ex.Message}");
        }
    }

    #region Package Methods
    public string AddPackage(string path, string packageName)
    {
        try
        {
            string projectPath = Path.Combine(path, "WebAPI");
            string csprojPath = Path.Combine(projectPath, "WebAPI.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"WebAPI.csproj not found for adding package({packageName}).");

            var doc = XDocument.Load(csprojPath);

            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == packageName);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in WebAPI project.";

            RunCommand(projectPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to WebAPI project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding pacgace to WebAPI project. \n\t Details:{ex.Message}");
        }
    }
    public string Restore(string path)
    {
        try
        {
            string projectPath = Path.Combine(path, "WebAPI");

            RunCommand(projectPath, "dotnet", "restore");
            return "OK: Restored WebAPI project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring WebAPI project. \n Details:{ex.Message}");
        }
    }
    #endregion

    #region Static Files
    public string GenerateExceptionHandler(string solutionPath)
    {
        string code = @"using Core.Enums;
using Core.Utils.ExceptionHandle.Exceptions;
using Core.Utils.ExceptionHandle.ProblemDetailModels;
using FluentValidation.Results;
using Newtonsoft.Json;
using Serilog;

namespace WebAPI.ExceptionHandler;

public class ExceptionHandleMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionHandleMiddleware(RequestDelegate next) => _next = next;


    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            await CatchExceptionAsync(context.Response, e);
        }
    }

    private Task CatchExceptionAsync(HttpResponse response, Exception exception)
    {
        response.ContentType = ""application/problem+json"";

        Type exceptionType = exception.GetType();

        if (exceptionType == typeof(ValidationRuleException)) return HandleValidationException(response, (ValidationRuleException)exception);
        if (exceptionType == typeof(DataAccessException)) return HandleDataAccessException(response, (DataAccessException)exception);
        if (exceptionType == typeof(BusinessException)) return HandleBusinessException(response, (BusinessException)exception);
        if (exceptionType == typeof(GeneralException)) return HandleGeneralException(response, (GeneralException)exception);

        return HandleOtherException(response, exception);
    }

    private Task HandleValidationException(HttpResponse response, ValidationRuleException exception)
    {
        Log.ForContext(""Target"", ""Validation"").Error(
            $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
            $""Type(Validation) \n"" +
            $""Location: {exception.LocationName} \n"" +
            $""Detail: {exception.Message} \n"" +
            $""Description:{exception.Description} \n"" +
            $""Parameters: {exception.Parameters} \n"" +
            $""------- ------- ------- FINISH ------- ------- -------\n\n"");

        response.StatusCode = StatusCodes.Status400BadRequest;
        IEnumerable<ValidationFailure> errors = exception.Errors;

        return response.WriteAsync(JsonConvert.SerializeObject(new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemDetailTypes.Validation.ToString(),
            Title = ""Validation error(s)"",
            Detail = exception.Message,
            Errors = errors
        }));
    }

    private Task HandleBusinessException(HttpResponse response, BusinessException exception)
    {
        Log.ForContext(""Target"", ""Business"").Error(
            $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
            $""Type(Business) \n"" +
            $""Location: {exception.LocationName} \n"" +
            $""Detail: {exception.Message} \n"" +
            $""Description:{exception.Description} \n"" +
            $""Parameters: {exception.Parameters} \n"" +
            $""Exception Raw: \n\n{exception.ToString()} \n"" +
            $""------- ------- ------- FINISH ------- ------- -------\n\n"");

        response.StatusCode = StatusCodes.Status409Conflict;

        return response.WriteAsync(JsonConvert.SerializeObject(new BusinessProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Type = ProblemDetailTypes.Business.ToString(),
            Title = ""Business Workflow Exception"",
            Detail = exception.Message
        }));
    }

    private Task HandleDataAccessException(HttpResponse response, DataAccessException exception)
    {
        Log.ForContext(""Target"", ""DataAccess"").Error(
            $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
            $""Type(DataAccess) \n"" +
            $""Location: {exception.LocationName} \n"" +
            $""Detail: {exception.Message} \n"" +
            $""Description:{exception.Description} \n"" +
            $""Parameters: {exception.Parameters} \n"" +
            $""Exception Raw: \n\n{exception.ToString()} \n"" +
            $""------- ------- ------- FINISH ------- ------- -------\n\n"");

        response.StatusCode = StatusCodes.Status500InternalServerError;

        return response.WriteAsync(JsonConvert.SerializeObject(new DataAccessProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = ProblemDetailTypes.DataAccess.ToString(),
            Title = ""Data Access Exception"",
            Detail = ""An error occurred during the process"",
        }));
    }

    private Task HandleGeneralException(HttpResponse response, GeneralException exception)
    {
        Log.ForContext(""Target"", ""Application"").Error(
            $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
            $""Type(General) \n"" +
            $""Location: {exception.LocationName} \n"" +
            $""Detail: {exception.Message} \n"" +
            $""Description:{exception.Description} \n"" +
            $""Parameters: {exception.Parameters} \n"" +
            $""Exception Raw: \n\n{exception.ToString()} \n"" +
            $""------- ------- ------- FINISH ------- ------- -------\n\n"");

        response.StatusCode = StatusCodes.Status500InternalServerError; // 500

        return response.WriteAsync(JsonConvert.SerializeObject(new Microsoft.AspNetCore.Mvc.ProblemDetails()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = ProblemDetailTypes.General.ToString(),
            Title = ""General exception"",
            Detail = ""An error occurred during the process""
        }));
    }

    private Task HandleOtherException(HttpResponse response, Exception exception)
    {
        Log.Error(
            $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
            $""Type(Others) \n"" +
            $""Detail: {exception.Message} \n"" +
            $""Exception Raw: \n\n{exception.ToString()} \n"" +
            $""------- ------- ------- FINISH ------- ------- -------\n\n"");

        response.StatusCode = StatusCodes.Status500InternalServerError; // 500

        return response.WriteAsync(JsonConvert.SerializeObject(new Microsoft.AspNetCore.Mvc.ProblemDetails()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = ProblemDetailTypes.General.ToString(),
            Title = ""General exception"",
            Detail = ""An error occurred during the process""
        }));
    }
}";

        string folderPath = Path.Combine(solutionPath, "WebAPI", "ExceptionHandler");
        return AddFile(folderPath, "ExceptionHandleMiddleware", code);
    }

    public string GenerateScalarSecuritySchemeTransformer(string solutionPath)
    {
        string code = @"
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebAPI.Utils;

public sealed class ScalarSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == ""Bearer""))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                [""Bearer""] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = ""bearer"",
                    In = ParameterLocation.Header,
                    BearerFormat = ""Json Web Token""
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = ""Bearer"", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                });
            }
        }
    }
}";

        string folderPath = Path.Combine(solutionPath, "WebAPI", "Utils");

        return AddFile(folderPath, "ScalarSecuritySchemeTransformer", code);
    }
    #endregion

    public string GenerateProgramCs(string solutionPath)
    {
        StringBuilder sb = new();
        sb.AppendLine("using WebAPI.ExceptionHandler;");
        sb.AppendLine("using WebAPI.Utils;");
        sb.AppendLine("using Autofac.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.AspNetCore.RateLimiting;");
        sb.AppendLine("using Autofac;");
        sb.AppendLine("using Business;");
        sb.AppendLine("using Core;");
        sb.AppendLine("using DataAccess;");
        sb.AppendLine("using Model;");
        sb.AppendLine("using Serilog;");
        sb.AppendLine("using Serilog.Filters;");
        sb.AppendLine("using Scalar.AspNetCore;");
        sb.AppendLine("using System.Threading.RateLimiting;");
        if (_appSetting.IsThereIdentity)
        {
            sb.AppendLine("using Model.Entities;");
            sb.AppendLine("using Core.Utils.Auth;");
            sb.AppendLine("using DataAccess.Contexts;");
            sb.AppendLine("using Microsoft.AspNetCore.Authentication.JwtBearer;");
            sb.AppendLine("using Microsoft.AspNetCore.Identity;");
            sb.AppendLine("using Microsoft.IdentityModel.Tokens;");
        }
        sb.AppendLine("");
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine("");
        AddCORS(ref sb);
        AddRateLimiter(ref sb);
        AddLogImplemantation(ref sb);
        AddLayerRegistrations(ref sb);
        AddAutofacModules(ref sb);
        if (_appSetting.IsThereIdentity)
        {
            var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
            string IdentityKeyType = identityTypeConfigs.IdentityKeyType;
            string IdentityUserType = identityTypeConfigs.IdentityUserType;
            string IdentityRoleType = identityTypeConfigs.IdentityRoleType;

            AddIdentityImplemantation(ref sb, IdentityUserType, IdentityRoleType);
            AddJWTImplemantation(ref sb);
        }
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddHealthChecks();");
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddControllers();");
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddOpenApi(options => {");
        sb.AppendLine("\toptions.AddDocumentTransformer<ScalarSecuritySchemeTransformer>();");
        sb.AppendLine("});");
        sb.AppendLine("");
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine("");
        sb.AppendLine("app.UseMiddleware<ExceptionHandleMiddleware>();");
        sb.AppendLine("");
        sb.AppendLine("//app.UseStaticFiles();");
        sb.AppendLine("");
        sb.AppendLine("if (app.Environment.IsDevelopment())");
        sb.AppendLine("{");
        sb.AppendLine("\tapp.MapOpenApi();");
        sb.AppendLine("\tapp.MapScalarApiReference();");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("app.UseHttpsRedirection();");
        sb.AppendLine("");
        sb.AppendLine("app.UseCors(\"policy_cors\");");
        sb.AppendLine("");
        sb.AppendLine("app.UseAuthentication();");
        sb.AppendLine("");
        sb.AppendLine("app.UseAuthorization();");
        sb.AppendLine("");
        sb.AppendLine("app.UseRateLimiter();");
        sb.AppendLine("");
        sb.AppendLine("app.MapControllers().RequireRateLimiting(\"policy_rate_limiter\");");
        sb.AppendLine("");
        sb.AppendLine("app.MapHealthChecks(\"/health\");");
        sb.AppendLine("");
        sb.AppendLine("app.Run();");

        string folderPath = Path.Combine(solutionPath, "WebAPI");

        return AddFile(folderPath, "Program", sb.ToString());
    }

    public string GenerateAppSettings(string solutionPath)
    {
        string code = @$"
{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""ConnectionStrings"": {{
    ""Database"": ""{_appSetting.DBConnectionString}""
  }},
  ""TokenSettings"": {{
    ""Audience"": ""sporoutine.com"",
    ""Issuer"": ""sporoutine.com"",
    ""AccessTokenExpiration"": 1440, // 1 day
    ""RefreshTokenExpiration"": 10080, // 7 day
    ""SecurityKey"": ""UÜVWXYZ0123456789-._@+/*|!,;()&#._TrDgoSJRCddnx57CnU_O43bIXGo6LwLr3em3YqAD8_NM37wMmNPuOr25NBYVfbtGwxtUrZLsgGL39UwKXjINCn0."",
    ""RefreshTokenTTL"": 7 // 7 günlük token süresince kaç kere refresh işlemi yapılacağını sınırlamak için
  }},
  ""AllowedHosts"": ""*""
}}
";

        string folderPath = Path.Combine(solutionPath, "WebAPI");
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, "appsettings.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File appsettings.json added to WebAPI project.";
            }
            else
            {
                return $"INFO: File appsettings.json already exists in WebAPI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file(appsettings.json) to WebAPI project. \n Details:{ex.Message}");
        }

    }

    public string GenerateControllers(string solutionPath)
    {
        var results = new List<string>();

        string folderPath = Path.Combine(solutionPath, "WebAPI", "Controllers");

        RoslynApiControllerGenerator roslynApiControllerGenerator = new RoslynApiControllerGenerator(_appSetting);

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(x => x.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields));

            string code_controller = roslynApiControllerGenerator.GeneraterController(entity, dtos);

            results.Add(AddFile(folderPath, $"{entity.Name}Controller", code_controller));
        }
        if (_appSetting.IsThereIdentity)
        {
            string code_AccountController = @"
using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using Model.Auth.Login;
using Model.Auth.RefreshAuth;
using Model.Auth.SignUp;

namespace WebAPI.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class AccountController : ControllerBase
{
    private readonly IAuthService _authService;
    public AccountController(IAuthService authService) => _authService = authService;


    [HttpPost(""Login"")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return Ok(result);
    }

    [HttpPost(""SignUp"")]
    public async Task<IActionResult> SignUp(SignUpRequest request)
    {
        var result = await _authService.SignUpAsync(request);

        return Ok(result);
    }

    [HttpPost(""RefreshAuth"")]
    public async Task<IActionResult> RefreshAuth(RefreshAuthRequest request)
    {
        var result = await _authService.RefreshAuthAsync(request);

        return Ok(result);
    }
}";

            results.Add(AddFile(folderPath, "AccountController", code_AccountController));
        }

        return string.Join("\n", results);
    }


    // **************** HEPLERS ****************
    private string AddFile(string folderPath, string fileName, string code)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"{fileName}.cs");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File {fileName} added to WebAPI project.";
            }
            else
            {
                return $"INFO: File {fileName} already exists in WebAPI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}) to WebAPI project. \n Details:{ex.Message}");
        }
    }

    private string RemoveFile(string folderPath, string fileName)
    {
        try
        {
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return $"OK: File {fileName} removed from WebAPI project.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist in WebAPI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) from WebAPI project. \n Details: {ex.Message}");
        }
    }

    private string RunCommand(string workingDirectory, string fileName, string arguments)
    {
        var processInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process!.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command failed: {error}");
            }
            else
            {
                return output;
            }
        }
    }



    #region Program.cs Implemantations
    private void AddCORS(ref StringBuilder sb)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- CORS -------");
        sb.AppendLine("builder.Services.AddCors(options =>");
        sb.AppendLine("{");
        sb.AppendLine("\toptions.AddPolicy(\"policy_cors\", builder =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tbuilder");
        sb.AppendLine("\t\t\t.AllowAnyOrigin()");
        sb.AppendLine("\t\t\t//.WithOrigins(\"https://www.frontend.com\")");
        sb.AppendLine("\t\t\t//.AllowCredentials() // AllowAnyOrigin and AllowCredentials cannot using together use with WithOrigins option ");
        sb.AppendLine("\t\t\t.WithHeaders(\"Content-Type\", \"Authorization\")");
        sb.AppendLine("\t\t\t.AllowAnyMethod()");
        sb.AppendLine("\t\t\t.SetPreflightMaxAge(TimeSpan.FromMinutes(10));");
        sb.AppendLine("\t});");
        sb.AppendLine("});");
        sb.AppendLine("// ------- CORS -------");
        sb.AppendLine("");
    }

    private void AddRateLimiter(ref StringBuilder sb)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- Rate Limiter -------");
        sb.AppendLine("builder.Services.AddRateLimiter(options =>");
        sb.AppendLine("{");
        sb.AppendLine("\toptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;");
        sb.AppendLine("\toptions.AddSlidingWindowLimiter(policyName: \"policy_rate_limiter\", slidingOptions =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tslidingOptions.PermitLimit = 30;");
        sb.AppendLine("\t\tslidingOptions.Window = TimeSpan.FromSeconds(5);");
        sb.AppendLine("\t\tslidingOptions.SegmentsPerWindow = 4;");
        sb.AppendLine("\t\tslidingOptions.QueueLimit = 5;");
        sb.AppendLine("\t\tslidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;");
        sb.AppendLine("\t});");
        sb.AppendLine("});");
        sb.AppendLine("// ------- Rate Limiter -------");
        sb.AppendLine("");
    }

    private void AddLogImplemantation(ref StringBuilder sb)
    {
        sb.AppendLine(@"
// ------- Logger Implementation -------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty(""Target"", (object p) => p.ToString() == ""Validation""))
        .WriteTo.File(""Logs/Validation/validation.log"", rollingInterval: RollingInterval.Day,
            outputTemplate: ""{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}""))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty(""Target"", (object p) => p.ToString() == ""Application""))
        .WriteTo.File(""Logs/Application/application.log"", rollingInterval: RollingInterval.Day,
            outputTemplate: ""{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}""))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty(""Target"", (object p) => p.ToString() == ""Business""))
        .WriteTo.File(""Logs/Business/business.log"", rollingInterval: RollingInterval.Day,
            outputTemplate: ""{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}""))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty(""Target"", (object p) => p.ToString() == ""DataAccess""))
        .WriteTo.File(""Logs/DataAccess/dataAccess.log"", rollingInterval: RollingInterval.Day,
            outputTemplate: ""{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}""))
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding(Matching.WithProperty<string>(""Target"", _ => true))
        .WriteTo.File(""Logs/Other/others.log"", rollingInterval: RollingInterval.Day,
            outputTemplate: ""{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}""))
    .CreateLogger();

builder.Host.UseSerilog();
// ------- Logger Implementation -------
");
    }

    private void AddLayerRegistrations(ref StringBuilder sb)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- Layer Registrations -------");
        sb.AppendLine("builder.Services.AddModelServices();");
        sb.AppendLine("builder.Services.AddCoreServices(builder.Configuration);");
        sb.AppendLine("builder.Services.AddDataAccessServices(builder.Configuration);");
        sb.AppendLine("builder.Services.AddBusinessServices(builder.Configuration);");
        sb.AppendLine("// ------- Layer Registrations -------");
        sb.AppendLine("");
    }

    private void AddAutofacModules(ref StringBuilder sb)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- Autofac Modules -------");
        sb.AppendLine("builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())");
        sb.AppendLine("\t.ConfigureContainer<ContainerBuilder>(builder =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tbuilder.RegisterModule(new Core.AutofacModule());");
        sb.AppendLine("\t\tbuilder.RegisterModule(new DataAccess.AutofacModule());");
        sb.AppendLine("\t\tbuilder.RegisterModule(new Business.AutofacModule());");
        sb.AppendLine("\t});");
        sb.AppendLine("// ------- Autofac Modules -------");
        sb.AppendLine("");
    }

    private void AddIdentityImplemantation(ref StringBuilder sb, string identityUserType, string identityRoleType)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- IDENTITY -------");
        sb.AppendLine("builder.Services");
        sb.AppendLine($"\t.AddIdentity<{identityUserType}, {identityRoleType}>(options =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\t// Default Lockout settings.");
        sb.AppendLine("\t\toptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);");
        sb.AppendLine("\t\toptions.Lockout.MaxFailedAccessAttempts = 5;");
        sb.AppendLine("\t\toptions.Lockout.AllowedForNewUsers = true;");
        sb.AppendLine("");
        sb.AppendLine("\t\toptions.SignIn.RequireConfirmedEmail = false;");
        sb.AppendLine("");
        sb.AppendLine("\t\toptions.Password.RequiredLength = 4;");
        sb.AppendLine("\t\toptions.Password.RequireDigit = false;");
        sb.AppendLine("\t\toptions.Password.RequireNonAlphanumeric = false;");
        sb.AppendLine("\t\toptions.Password.RequireLowercase = false;");
        sb.AppendLine("\t\toptions.Password.RequireUppercase = false;");
        sb.AppendLine("");
        sb.AppendLine("\t\toptions.User.RequireUniqueEmail = false;");
        sb.AppendLine("\t\toptions.User.AllowedUserNameCharacters = \"abcçdefgğhiıjklmnoöpqrsştuüvwxyzABCÇDEFGĞHIİJKLMNOÖPQRSŞTUÜVWXYZ0123456789-._@+/*|!,;:()&#?[] \";");
        sb.AppendLine("\t})");
        sb.AppendLine("\t.AddEntityFrameworkStores<AppDbContext>()");
        sb.AppendLine("\t.AddDefaultTokenProviders();");
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddAuthorization();");
        sb.AppendLine("// ------- IDENTITY -------");
        sb.AppendLine("");
    }

    private void AddJWTImplemantation(ref StringBuilder sb)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- JWT Implementation -------");
        sb.AppendLine("TokenSettings tokenSettings = builder.Configuration.GetSection(\"TokenSettings\").Get<TokenSettings>()!;");
        sb.AppendLine("builder.Services.AddSingleton(tokenSettings);");
        sb.AppendLine("");
        sb.AppendLine("builder.Services");
        sb.AppendLine("\t.AddAuthentication(options =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\toptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;");
        sb.AppendLine("\t\toptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;");
        sb.AppendLine("\t\toptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;");
        sb.AppendLine("\t})");
        sb.AppendLine("\t.AddJwtBearer(options =>");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\toptions.TokenValidationParameters = new TokenValidationParameters");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tValidateIssuerSigningKey = true,");
        sb.AppendLine("\t\t\tValidateLifetime = true,");
        sb.AppendLine("\t\t\tValidateAudience = true,");
        sb.AppendLine("\t\t\tValidateIssuer = true,");
        sb.AppendLine("\t\t\tValidIssuer = tokenSettings.Issuer,");
        sb.AppendLine("\t\t\tValidAudience = tokenSettings.Audience,");
        sb.AppendLine("\t\t\tIssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSettings.SecurityKey))");
        sb.AppendLine("\t\t};");
        sb.AppendLine("\t});");
        sb.AppendLine("// ------- JWT Implementation -------");
        sb.AppendLine("");
    }
    #endregion
}
