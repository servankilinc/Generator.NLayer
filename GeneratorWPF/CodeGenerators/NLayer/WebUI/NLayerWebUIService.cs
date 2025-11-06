using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.WebUI;

public class NLayerWebUIService
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly AppSetting _appSetting;
    public NLayerWebUIService(AppSetting appSetting)
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
            string projectPath = Path.Combine(path, "WebUI");
            string projectViewsPath = Path.Combine(path, "WebUI", "Views");
            string projectViewsSharedPath = Path.Combine(path, "WebUI", "Views", "Shared");
            string csprojPath = Path.Combine(projectPath, "WebUI.csproj");

            if (Directory.Exists(projectPath) && File.Exists(csprojPath))
                return "INFO: WebUI layer project already exists.";

            RunCommand(path, "dotnet", $"new mvc -n WebUI");
            RunCommand(path, "dotnet", $"sln {solutionName}.sln add WebUI/WebUI.csproj");
            RunCommand(projectPath, "dotnet", $"add reference ../Business/Business.csproj");

            RemoveFile(projectPath, "Program.cs");
            RemoveFile(projectPath, "appsettings.json");
            RemoveFile(projectViewsPath, "_ViewImports.cshtml");
            RemoveFile(projectViewsSharedPath, "_Layout.cshtml");

            return "OK: WebUI Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the WebUI project. \n\t Details:{ex.Message}");
        }
    }

    #region Package Methods
    public string AddPackage(string path, string packageName)
    {
        try
        {
            string projectPath = Path.Combine(path, "WebUI");
            string csprojPath = Path.Combine(projectPath, "WebUI.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"WebUI.csproj not found for adding package({packageName}).");

            var doc = XDocument.Load(csprojPath);

            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == packageName);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in WebUI project.";

            RunCommand(projectPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to WebUI project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding pacgace to WebUI project. \n\t Details:{ex.Message}");
        }
    }
    public string Restore(string path)
    {
        try
        {
            string projectPath = Path.Combine(path, "WebUI");

            RunCommand(projectPath, "dotnet", "restore");
            return "OK: Restored WebUI project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring WebUI project. \n Details:{ex.Message}");
        }
    }
    public string CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir)) 
            Directory.CreateDirectory(destinationDir);

        //return $"INFO: Directory {sourceDir} already exists in WebUI project.";

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string targetFilePath = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, targetFilePath, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string targetSubDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, targetSubDir);
        }

        return $"OK: Directory {sourceDir} added to WebUI project.";
    }
    #endregion

    #region Static Files
    public string GenerateUtils(string solutionPath)
    {
        List<string> results = new List<string>();
        string code_ValidationFilter = @"using Core.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace WebUI.Utils.ActionFilters;

public class ValidationFilter<TModel> : ActionFilterAttribute
{
    private readonly IEnumerable<IValidator<TModel>> _validators;
    public ValidationFilter(IEnumerable<IValidator<TModel>> validators) => _validators = validators;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_validators.Any()) return;

        var model = (TModel?)context.ActionArguments.Values.FirstOrDefault(f => f.GetType() == typeof(TModel));
        if (model == null) return;

        IEnumerable<FluentValidation.Results.ValidationFailure> validationFailures = _validators
            .Select(validator => validator.Validate(model))
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .ToList();

        if (!validationFailures.Any()) return;

        var isJsonRequest =
            context.HttpContext.Request.Headers[""Accept""].ToString().Contains(""application/json"") ||
            context.HttpContext.Request.Headers[""X-Requested-With""].ToString().Contains(""XMLHttpRequest"") ||
            context.HttpContext.Request.ContentType?.Contains(""application/json"") == true;


        if (isJsonRequest)
        {
            var problemDetails = new Core.Utils.ExceptionHandle.ProblemDetailModels.ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = ProblemDetailTypes.Validation.ToString(),
                Title = ""Validation error(s)"",
                Detail = ""One or more validation errors occurred."",
                Errors = validationFailures
            };

            context.Result = new BadRequestObjectResult(problemDetails);
        }
        else
        {
            foreach (var failure in validationFailures)
            {
                context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            string? action = context.ActionDescriptor.RouteValues[""action""];
            context.Result = new ViewResult
            {
                ViewName = action,
                ViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider(), modelState: context.ModelState)
                {
                    Model = model
                }
            };
        }
    }
}";

        string code_HttpContextExtensions = @"using System.Text;

namespace WebUI.Utils.Extensions;

public static class HttpContextExtensions
{
    public static bool IsJsonRequest(this HttpContext httpContext)
    {
        return
            httpContext.Request.Headers[""X-Request-Provider""].ToString().ToLowerInvariant() == ""json"" ||
            httpContext.Request.Headers[""Accept""].ToString().Contains(""application/json"") ||
            httpContext.Request.Headers[""X-Requested-With""].ToString().Contains(""XMLHttpRequest"") ||
            httpContext.Request.ContentType?.Contains(""application/json"") == true;
    }

    public static string GetUrl(this HttpContext httpContext)
    {
        string SchemeDelimiter = Uri.SchemeDelimiter;
        var scheme = httpContext.Request.Scheme ?? string.Empty;
        var host = httpContext.Request.Host.Value ?? string.Empty;
        var pathBase = httpContext.Request.PathBase.Value ?? string.Empty;
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var queryString = httpContext.Request.QueryString.Value ?? string.Empty;

        var length = scheme.Length + SchemeDelimiter.Length + host.Length + pathBase.Length + path.Length + queryString.Length;

        return new StringBuilder(length)
            .Append(scheme)
            .Append(SchemeDelimiter)
            .Append(host)
            .Append(pathBase)
            .Append(path)
            .Append(queryString)
            .ToString();
    }

    public static string GetPath(this HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        return path.Trim();
    }

    public static string GetBasePath(this HttpContext httpContext)
    {
        string? controller = httpContext.GetRouteData().Values[""controller""]?.ToString()?.Trim();
        string? action = httpContext.GetRouteData().Values[""action""]?.ToString()?.Trim();

        return $""/{controller}/{action}"";
    }


    public static string GetLightMode(this HttpContext httpContext)
    {
        return httpContext.Request.Cookies[""light_mode""] ?? ""system"";
    }

    public static void SetLightMode(this HttpContext httpContext, string mode)
    {
        httpContext.Response.Cookies.Append(""light_mode"", mode, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(7),
        });
    }
}";

        string folderPathValidationFilter = Path.Combine(solutionPath, "WebUI", "Utils", "ActionFilters");
        string folderPathHttpContextExtensions = Path.Combine(solutionPath, "WebUI", "Utils", "Extensions");

        results.Add(AddFile(folderPathValidationFilter, "ValidationFilter", code_ValidationFilter));
        results.Add(AddFile(folderPathHttpContextExtensions, "HttpContextExtensions", code_HttpContextExtensions));

        return string.Join("\n", results);
    }

    public string GenerateExceptionHandler(string solutionPath)
    {
        string code = @"using Core.Enums;
using Core.Utils.ExceptionHandle.Exceptions;
using Core.Utils.ExceptionHandle.ProblemDetailModels;
using FluentValidation.Results;
using Newtonsoft.Json;
using Serilog;
using WebUI.Utils.Extensions;

namespace WebUI.ExceptionHandler;

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
            if (context.IsJsonRequest())
            {
                await CatchJsonExceptionAsync(context.Response, e);
            }
            else
            {
                CatchPageException(context.Response, e);
            }
        }
    }


    private void CatchPageException(HttpResponse response, Exception exception)
    {
        //response.Clear();

        Type exceptionType = exception.GetType();

        if (exceptionType == typeof(ValidationRuleException))
        {
            var validException = (ValidationRuleException)exception;
            Log.ForContext(""Target"", ""Validation"").Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(Validation) \n"" +
                $""Location: {validException.LocationName} \n"" +
                $""Detail: {validException.Message} \n"" +
                $""Description:{validException.Description} \n"" +
                $""Parameters: {validException.Parameters} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");

            response.Redirect(""/Error/InvalidProcess"");
        }
        else if (exceptionType == typeof(BusinessException))
        {
            var businessException = (BusinessException)exception;
            Log.ForContext(""Target"", ""Business"").Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(Business) \n"" +
                $""Location: {businessException.LocationName} \n"" +
                $""Detail: {businessException.Message} \n"" +
                $""Description:{businessException.Description} \n"" +
                $""Parameters: {businessException.Parameters} \n"" +
                $""Exception Raw: \n\n{businessException.ToString()} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");

            response.Redirect(""/Error/InvalidProcess"");
        }
        else if (exceptionType == typeof(GeneralException))
        {
            var generalException = (GeneralException)exception;
            Log.ForContext(""Target"", ""Application"").Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(General) \n"" +
                $""Location: {generalException.LocationName} \n"" +
                $""Detail: {generalException.Message} \n"" +
                $""Description:{generalException.Description} \n"" +
                $""Parameters: {generalException.Parameters} \n"" +
                $""Exception Raw: \n\n{generalException.ToString()} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");

            response.Redirect(""/Error/InternalServer"");
        }
        else if (exceptionType == typeof(DataAccessException))
        {
            var dataAccessException = (DataAccessException)exception;
            Log.ForContext(""Target"", ""DataAccess"").Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(DataAccess) \n"" +
                $""Location: {dataAccessException.LocationName} \n"" +
                $""Detail: {dataAccessException.Message} \n"" +
                $""Description:{dataAccessException.Description} \n"" +
                $""Parameters: {dataAccessException.Parameters} \n"" +
                $""Exception Raw: \n\n{dataAccessException.ToString()} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");

            response.Redirect(""/Error/InternalServer"");
        }
        else if (response.StatusCode == 404 && !response.HasStarted)
        {
            Log.Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(Not Found) \n"" +
                $""Detail: {exception.Message} \n"" +
                $""Exception Raw: \n\n{exception.ToString()} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");
            response.Redirect(""/Error/NotFound"");
        }
        else
        {
            Log.Error(
                $""\n\n------- ------- ------- Start ------- ------- ------- \n"" +
                $""Type(Others) \n"" +
                $""Detail: {exception.Message} \n"" +
                $""Exception Raw: \n\n{exception.ToString()} \n"" +
                $""------- ------- ------- FINISH ------- ------- -------\n\n"");

            response.Redirect(""/Error/InternalServer"");
        }
    }

    private Task CatchJsonExceptionAsync(HttpResponse response, Exception exception)
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

        string folderPath = Path.Combine(solutionPath, "WebUI", "ExceptionHandler");
        return AddFile(folderPath, "ExceptionHandleMiddleware", code);
    }

    public string GenerateSideMenuViewComponent(string solutionPath)
    {
        string section1 = @"
using Microsoft.AspNetCore.Mvc;
using WebUI.Models.UI;

namespace WebUI.ViewComponents;

public class SideMenuViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var menuItems = new List<MenuItem>()
        {
            new MenuItem
            {
                Title = ""Dashboard"",
                Icon = ""fa-brands fa-magento"",
                Path = ""/Home/Index"",
                Type = 1,
            },";

        string section2 = @"
        };
        return View(menuItems);
    }
}";

        StringBuilder stringBuilder = new StringBuilder();

        List<Entity> entities = _entityRepository.GetAll();

        foreach (Entity entity in entities)
        {
            string groupName = entities.FindIndex(f => f.Id == entity.Id) == 0 ? "GroupName = \"Pages\"," : "";

            stringBuilder.AppendLine($@"
      new MenuItem
            {{
                Title = ""{entity.Name}"",
                Icon = ""fa-regular fa-folder-open"",
                Type = 0,
                {groupName}
                SubMenuItems = new List<MenuItem>()
                {{
                    new MenuItem
                    {{
                        Title = ""Managment"",
                        Icon = ""fa-regular fa-file-lines"",
                        Path = ""/{entity.Name}/Index"",
                        Type = 1,
                    }},
                    new MenuItem
                    {{
                        Title = ""Create"",
                        Icon = ""fa-solid fa-file-circle-plus"",
                        Path= ""/{entity.Name}/Create"",
                        Type = 1,
                    }}
                }}
            }},");
        }
        string code = section1 + stringBuilder.ToString() + section2;
        string folderPathMenuItem = Path.Combine(solutionPath, "WebUI", "ViewComponents");
        return AddFile(folderPathMenuItem, "SideMenuViewComponent", code);
    }
    #endregion

    public string Generate_wwwroot(string solutionPath)
    {
        if (string.IsNullOrEmpty(_appSetting.Path)) return "Warning: Not Found Destionation Path to Generation wwwroot files";

        var results = new List<string>();

        string basePath = AppContext.BaseDirectory;

        string sourcePath_assets = Path.GetFullPath(Path.Combine(basePath, @"CodeGenerators\NLayer\WebUI\wwwroot\assets"));
        string sourcePath_css = Path.GetFullPath(Path.Combine(basePath, @"CodeGenerators\NLayer\WebUI\wwwroot\css"));
        string sourcePath_js = Path.GetFullPath(Path.Combine(basePath, @"CodeGenerators\NLayer\WebUI\wwwroot\js"));
        string sourcePath_lib = Path.GetFullPath(Path.Combine(basePath, @"CodeGenerators\NLayer\WebUI\wwwroot\lib"));

        string destPath_assets = Path.GetFullPath(Path.Combine(_appSetting.Path, _appSetting.SolutionName, @"WebUI\wwwroot\assets"));
        string destPath_css = Path.GetFullPath(Path.Combine(_appSetting.Path, _appSetting.SolutionName, @"WebUI\wwwroot\css"));
        string destPath_js = Path.GetFullPath(Path.Combine(_appSetting.Path, _appSetting.SolutionName, @"WebUI\wwwroot\js"));
        string destPath_lib = Path.GetFullPath(Path.Combine(_appSetting.Path, _appSetting.SolutionName, @"WebUI\wwwroot\lib"));

        results.Add(this.CopyDirectory(sourcePath_assets, destPath_assets));
        results.Add(this.CopyDirectory(sourcePath_css, destPath_css));
        results.Add(this.CopyDirectory(sourcePath_js, destPath_js));
        results.Add(this.CopyDirectory(sourcePath_lib, destPath_lib));
        return "wwwroot generated";
    }

    public string GenerateViewModels(string solutionPath)
    {
        var results = new List<string>();

        RoslynWebUIViewModelGenerator roslynWebUIControllerGenerator = new RoslynWebUIViewModelGenerator(_appSetting);

        var entities = _entityRepository.GetAll(f => f.Control == false);

        foreach (var entity in entities)
        {
            string code_VMIndex = roslynWebUIControllerGenerator.GenerateViewModelIndex(entity);
            string code_VMCreate = roslynWebUIControllerGenerator.GenerateViewModelCreate(entity);
            string code_VMUpdate = roslynWebUIControllerGenerator.GenerateViewModelUpdate(entity);

            string folderPath = Path.Combine(solutionPath, "WebUI", "Models", "ViewModels", $"{entity.Name}_");

            results.Add(AddFile(folderPath, $"{entity.Name}ViewModel", code_VMIndex));
            results.Add(AddFile(folderPath, $"{entity.Name}CreateViewModel", code_VMCreate));
            results.Add(AddFile(folderPath, $"{entity.Name}UpdateViewModel", code_VMUpdate));
        }

        #region UI MenuItem
        string code_MenuItem = @"namespace WebUI.Models.UI;

public class MenuItem
{
    public string Title { get; set; } = ""Menu Item"";
    public int Type { get; set; } // 0 = group, 1 = route
    public string? Path { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<MenuItem>? SubMenuItems { get; set; }
}";

        string folderPathMenuItem = Path.Combine(solutionPath, "WebUI", "Models", "UI");
        results.Add(AddFile(folderPathMenuItem, "MenuItem", code_MenuItem));
        #endregion

        return string.Join("\n", results);
    }

    public string GenerateControllers(string solutionPath)
    {
        var results = new List<string>();

        string folderPath = Path.Combine(solutionPath, "WebUI", "Controllers");

        RoslynWebUIControllerGenerator roslynWebUIControllerGenerator = new RoslynWebUIControllerGenerator(_appSetting);

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(x => x.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields));

            string code_controller = roslynWebUIControllerGenerator.GeneraterController(entity, dtos);

            results.Add(AddFile(folderPath, $"{entity.Name}Controller", code_controller));
        }

        // Accout Controller
        if (_appSetting.IsThereIdentiy)
        {
            string code_AccountController = @"using Business.Abstract;
using Core.Utils.ExceptionHandle.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Model.Auth.Login;
using Model.Auth.SignUp;
using WebUI.Utils.ActionFilters;

namespace WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }


        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginRequest();
            return View(model);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilter<LoginRequest>))]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            try
            {
                await _authService.LoginWebBaseAsync(loginRequest);

                return RedirectToAction(""Index"", ""Home"");
            }
            catch (Exception ex)
            {
                Type exType = ex.GetType();
                if (exType == typeof(BusinessException))
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, ""İşlem Sırasında Bir Sorun Oluştu. Lütfen Daha Sonra Tekrar Deneyiniz!"");
                }
                return View(loginRequest);
            }
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilter<SignUpRequest>))]
        public async Task<IActionResult> SignUp(SignUpRequest signUpRequest)
        {
            try
            {
                await _authService.SignUpWebBaseAsync(signUpRequest);

                return RedirectToAction(""Index"", ""Home"");
            }
            catch (Exception ex)
            {
                Type exType = ex.GetType();
                if (exType == typeof(BusinessException))
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, ""İşlem Sırasında Bir Sorun Oluştu. Lütfen Daha Sonra Tekrar Deneyiniz!"");
                }
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(""Login"", ""Account"");
        }
    }
}";

            results.Add(AddFile(folderPath, "AccountController", code_AccountController));
        }

        // Error Controller
        string code_ErrorController = @"using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Forbidden()
        {
            return View();
        }

        public IActionResult InternalServer()
        {
            return View();
        }

        public IActionResult InvalidProcess()
        {
            return View();
        }

        public new IActionResult NotFound()
        {
            return View();
        }
    }
}";
        results.Add(AddFile(folderPath, "ErrorController", code_ErrorController));

        // UI Controller
        string code_UIController = @"using Microsoft.AspNetCore.Mvc;
using WebUI.Utils.Extensions;

namespace WebUI.Controllers;

public class UIController : Controller
{
    public IActionResult SetLightMode(string mode, string? returnUrl)
    {
        HttpContext.SetLightMode(mode);

        if (string.IsNullOrEmpty(returnUrl)) return RedirectToAction(""Index"", ""Home"");

        var uri = new Uri(returnUrl);
        if (uri.Host != Request.Host.Host) return RedirectToAction(""Index"", ""Home"");

        if (Request.Path.HasValue && (uri.LocalPath == Request.Path.Value)) return RedirectToAction(""Index"", ""Home"");

        return Redirect(returnUrl);
    }
}";
        results.Add(AddFile(folderPath, "UIController", code_UIController));

        return string.Join("\n", results);
    }

    public string GenerateProgramCs(string solutionPath)
    {
        StringBuilder sb = new();
        sb.AppendLine("using Autofac;");
        sb.AppendLine("using Autofac.Extensions.DependencyInjection;");
        sb.AppendLine("using Business;");
        sb.AppendLine("using Core;");
        sb.AppendLine("using DataAccess;");
        sb.AppendLine("using FluentValidation.AspNetCore;");
        sb.AppendLine("using Microsoft.AspNetCore.RateLimiting;");
        sb.AppendLine("using Model;");
        sb.AppendLine("using Serilog;");
        sb.AppendLine("using Serilog.Filters;");
        sb.AppendLine("using System.Threading.RateLimiting;");
        sb.AppendLine("using WebUI.ExceptionHandler;");
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine("using Core.Utils.Auth;");
            sb.AppendLine("using DataAccess.Contexts;");
            sb.AppendLine("using Microsoft.AspNetCore.Identity;");
            sb.AppendLine("using Model.Entities;");
        }

        sb.AppendLine("");
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();");
        sb.AppendLine("");
        sb.AppendLine("builder.Services.AddFluentValidationAutoValidation();");
        sb.AppendLine("builder.Services.AddFluentValidationClientsideAdapters();");
        sb.AppendLine("");
        sb.AppendLine("");
        AddCORS(ref sb);
        AddRateLimiter(ref sb);
        AddLogImplemantation(ref sb);
        AddLayerRegistrations(ref sb);
        AddAutofacModules(ref sb);

        if (_appSetting.IsThereIdentiy)
        {
            var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);

            AddIdentityImplemantation(ref sb, identityTypeConfigs.IdentityUserType, identityTypeConfigs.IdentityRoleType);
        }
        AddCookieOptions(ref sb);
        AddActionFilters(ref sb);
        sb.AppendLine("");
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine("");
        sb.AppendLine("app.UseMiddleware<ExceptionHandleMiddleware>();");
        sb.AppendLine("");
        sb.AppendLine("//app.UseStaticFiles();");
        sb.AppendLine("");
        sb.AppendLine("if (!app.Environment.IsDevelopment())");
        sb.AppendLine("{");
        sb.AppendLine("\tapp.UseExceptionHandler(\"/Home/InvalidProcess\");");
        sb.AppendLine("");
        sb.AppendLine("\t// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.");
        sb.AppendLine("\tapp.UseHsts();");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("app.UseStatusCodePagesWithReExecute(\"/Error/NotFound\");");
        sb.AppendLine("");
        sb.AppendLine("app.UseHttpsRedirection();");
        sb.AppendLine("");
        sb.AppendLine("app.UseCors(\"policy_cors\");");
        sb.AppendLine("");
        sb.AppendLine("app.UseRouting();");
        sb.AppendLine("");
        sb.AppendLine("app.UseAuthentication();");
        sb.AppendLine("app.UseAuthorization();");
        sb.AppendLine("");
        sb.AppendLine("app.MapStaticAssets();");
        sb.AppendLine("");
        sb.AppendLine("app.UseRateLimiter();");
        sb.AppendLine("");
        sb.AppendLine(@"
app.MapControllerRoute(
    name: ""default"",
    pattern: ""{controller=Home}/{action=Index}/{id?}"")
    .WithStaticAssets()
    .RequireRateLimiting(""policy_rate_limiter"");
");
        sb.AppendLine("");
        sb.AppendLine("app.Run();");

        string folderPath = Path.Combine(solutionPath, "WebUI");

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
  ""AllowedHosts"": ""*""
}}

";
        string folderPath = Path.Combine(solutionPath, "WebUI");
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
                return $"OK: File appsettings.json added to WebUI project.";
            }
            else
            {
                return $"INFO: File appsettings.json already exists in WebUI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file(appsettings.json) to WebUI project. \n Details:{ex.Message}");
        }

    }


    public string GenerateViews(string solutionPath)
    {
        var results = new List<string>();

        string folderPath = Path.Combine(solutionPath, "WebUI", "Views");

        ViewGenerator viewGenerator = new ViewGenerator(_appSetting);

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            string code_indexPage = viewGenerator.GenerateIndexPage(entity);
            string code_createPage = viewGenerator.GenerateCreatePage(entity);
            string code_createFormPage = viewGenerator.GenerateCreateFormPage(entity);
            string code_updateFormPage = viewGenerator.GenerateUpdateFormPage(entity);

            string viewFolderPath = Path.Combine(solutionPath, "WebUI", "Views", entity.Name);
            string viewPartialFolderPath = Path.Combine(solutionPath, "WebUI", "Views", entity.Name, "Partials");

            results.Add(AddFileByExt(viewFolderPath, $"Index.cshtml", code_indexPage));
            results.Add(AddFileByExt(viewFolderPath, $"Create.cshtml", code_createPage));

            results.Add(AddFileByExt(viewPartialFolderPath, $"CreateForm.cshtml", code_createFormPage));
            results.Add(AddFileByExt(viewPartialFolderPath, $"UpdateForm.cshtml", code_updateFormPage));
        }

        #region Account Views
        if (_appSetting.IsThereIdentiy)
        {
            string code_Login = @"@using Model.Auth.Login
@model LoginRequest
@{
    Layout = ""_LayoutBase"";
    ViewData[""Title""] = ""Login"";
}

<div class=""row justify-content-center"">
    <div class=""col-lg-4 mt-12"">
        <div class=""card"">
            <div class=""card-header border-bottom mb-6"">
                <div class=""app-brand justify-content-center"">
                    <a asp-controller=""Home"" asp-action=""Index"" class=""d-flex gap-2"">
                        <h4 class=""text-primary m-0"">
                            Login
                        </h4>
                    </a>
                </div>
            </div>
            <div class=""card-body"">
                <form asp-controller=""Account"" asp-action=""Login"" method=""post"" class=""mb-6"">

                    <div class=""mb-6"">
                        <label asp-for=""Email"" class=""form-label"">Email</label>
                        <input asp-for=""Email"" class=""form-control"" autofocus />
                        <span asp-validation-for=""Email""></span>
                    </div>
                    <div class=""mb-6"">
                        <label asp-for=""Password"" class=""form-label"">Password</label>
                        <div class=""input-group"">
                            <input asp-for=""Password"" class=""form-control"" placeholder=""&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;"" />
                            <span asp-validation-for=""Password""></span>
                        </div>
                    </div>
                    <div class=""mb-8"">
                        <div class=""d-flex justify-content-between"">
                            <div class=""form-check mb-0"">
                                <input class=""form-check-input"" type=""checkbox"" id=""remember-me"" />
                                <label class=""form-check-label"" for=""remember-me""> Remember Me </label>
                            </div>
                            <a asp-controller=""Error"" asp-action=""NotFound"">
                                <span>Forgot Password?</span>
                            </a>
                        </div>
                    </div>
                    <div asp-validation-summary=""All"" class=""text-danger mb-3""></div>
                    <div class=""mb-6"">
                        <button type=""submit"" class=""btn btn-primary d-grid w-100"">Login</button>
                    </div>
                </form>
                <p class=""text-center"">
                    <span>New on our platform?</span>
                    <a asp-controller=""Account"" asp-action=""SignUp"">
                        <span>Create an account</span>
                    </a>
                </p>
            </div>
        </div>
    </div>
</div>";

            string code_Signup = @"@using Model.Auth.SignUp
@model SignUpRequest
@{
    Layout = ""_LayoutBase"";
    ViewData[""Title""] = ""SignUp"";
}

<div class=""row justify-content-center"">
    <div class=""col-lg-4 mt-12"">
        <div class=""card"">
            <div class=""card-header border-bottom mb-6"">
                <div class=""app-brand justify-content-center"">
                    <a asp-controller=""Home"" asp-action=""Index"" class=""d-flex gap-2"">
                        <h4 class=""text-primary m-0"">
                            Create New Account
                        </h4>
                    </a>
                </div>
            </div>
            <div class=""card-body"">
                <form asp-controller=""Account"" asp-action=""SignUp"" method=""post"" class=""mb-6"">
                    <div class=""mb-6"">
                        <label asp-for=""Email"" class=""form-label"">Email</label>
                        <input asp-for=""Email"" class=""form-control"" autofocus />
                        <span asp-validation-for=""Email""></span>
                    </div>
                    <div>
                        <label asp-for=""Password"" class=""form-label"">Password</label>
                        <div class=""input-group"">
                            <input asp-for=""Password"" type=""password"" class=""form-control"" placeholder=""&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;"" /> 
                            <span asp-validation-for=""Password""></span>
                        </div>
                    </div>
                    <div class=""my-7"">
                        <div class=""form-check mb-0"">
                            <input class=""form-check-input"" type=""checkbox"" id=""terms-conditions"" name=""terms"" />
                            <label class=""form-check-label"" for=""terms-conditions"">
                                I agree to
                                <a href=""javascript:void(0);"">privacy policy & terms</a>
                            </label>
                        </div>
                    </div>
                    <button type=""submit"" class=""btn btn-primary d-grid w-100"">Sign up</button>
                </form>

                <p class=""text-center"">
                    <span>Already have an account?</span>
                    <a asp-controller=""Account"" asp-action=""Login"">
                        <span>Login instead</span>
                    </a>
                </p>
            </div>
        </div>
    </div>
</div>";

            if (_appSetting.IsThereUser && _appSetting.UserEntityId != default)
            {
                Entity? userEntity = entities.FirstOrDefault(f => f.Id == _appSetting.UserEntityId);

                if (userEntity != default)
                { 
                    code_Signup = viewGenerator.GenerateSignUpPage(userEntity);
                }
            }

            string accountViewsPath = Path.Combine(solutionPath, "WebUI", "Views", "Account");
            results.Add(AddFileByExt(accountViewsPath, "Login.cshtml", code_Login));
            results.Add(AddFileByExt(accountViewsPath, "SignUp.cshtml", code_Signup));
        } 
        #endregion


        #region Error Views
        string code_Forbidden = @"@{
    ViewData[""Title""] = ""Forbiden Error"";
    Layout = ""_LayoutBase"";
}

<div class=""misc-wrapper"">
    <h1 class=""mb-2 mx-2"" style=""line-height: 6rem;font-size: 6rem;"">401</h1>
    <h4 class=""mb-2 mx-2"">You are not authorized! 🔐</h4>
    <p class=""mb-6 mx-2"">You don’t have permission to access this page. Go Home!</p>
    <a href=""index.html"" class=""btn btn-primary"">Back to home</a>
    <div class=""mt-6"">
        <img src=""~/assets/img/illustrations/girl-with-laptop-light.png"" alt=""page-misc-not-authorized-light"" width=""500"" class=""img-fluid"" >
    </div>
</div>";
        string code_InternalServer = @"@{
    ViewData[""Title""] = ""Server Error"";
    Layout = ""_LayoutBase"";
}

<div class=""container-xxl container-p-y"">
    <div class=""misc-wrapper"">
        <h1 class=""mb-2 mx-2"" style=""line-height: 6rem;font-size: 6rem;"">500</h1>
        <h4 class=""mb-2 mx-2"">Internal Servver Error ⚠️</h4>
        <p class=""mb-6 mx-2"">we couldn't handle the process you are doing for</p>
        <a href=""/"" class=""btn btn-primary"">Back to home</a>
        <div class=""mt-6"">
            <img src=""~/assets/img/illustrations/girl-doing-yoga-light.png"" alt=""page-misc-error-light"" width=""500"" class=""img-fluid"">
        </div>
    </div>
</div>";
        string code_InvalidProcess = @"@{
    ViewData[""Title""] = ""Invalid Process"";
    Layout = ""_LayoutBase"";
}

<div class=""container-xxl container-p-y"">
    <div class=""misc-wrapper"">
        <h1 class=""mb-2 mx-2"" style=""line-height: 6rem;font-size: 6rem;"">500</h1>
        <h4 class=""mb-2 mx-2"">Invalid Process Error ⚠️</h4>
        <p class=""mb-6 mx-2"">İşlem Sırasında Bir Sorun Oluştu</p>
        <a href=""/"" class=""btn btn-primary"">Back to home</a>
        <div class=""mt-6"">
            <img src=""~/assets/img/illustrations/girl-doing-yoga-light.png"" alt=""page-misc-error-light"" width=""500"" class=""img-fluid"">
        </div>
    </div>
</div>";
        string code_NotFound = @"@{
    ViewData[""Title""] = ""Not Found"";
    Layout = ""_LayoutBase"";
}
<div class=""container-xxl container-p-y"">
    <div class=""misc-wrapper"">
        <h1 class=""mb-2 mx-2"" style=""line-height: 6rem;font-size: 6rem;"">404</h1>
        <h4 class=""mb-2 mx-2"">Page Not Found️ ⚠️</h4>
        <p class=""mb-6 mx-2"">we couldn't find the page you are looking for</p>
        <a href=""/"" class=""btn btn-primary"">Back to home</a>
        <div class=""mt-6"">
            <img src=""~/assets/img/illustrations/page-misc-error-light.png"" alt=""page-misc-error-light"" width=""500"" class=""img-fluid"">
        </div>
    </div>
</div>";

        string errorViewsPath = Path.Combine(solutionPath, "WebUI", "Views", "Error");
        results.Add(AddFileByExt(errorViewsPath, "Forbidden.cshtml", code_Forbidden));
        results.Add(AddFileByExt(errorViewsPath, "InternalServer.cshtml", code_InternalServer));
        results.Add(AddFileByExt(errorViewsPath, "InvalidProcess.cshtml", code_InvalidProcess));
        results.Add(AddFileByExt(errorViewsPath, "NotFound.cshtml", code_NotFound));
        #endregion
         
        #region Layout 
        string code_Layout = @"@{
    var currentUrl = Context.GetUrl();
    var lightMode = Context.GetLightMode();
}
<!DOCTYPE html>
<html class=""@(lightMode == ""dark"" ? ""dark-style"" : ""light-style"") layout-menu-fixed layout-menu-expanded overflow-x-hidden""
      dir=""ltr""
      data-theme=""theme-default""
      data-style=""light""
      lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - MyApp</title>
    <link rel=""icon"" type=""image/x-icon"" href=""~/assets/img/favicon/favicon.ico"" />

    <partial name=""_LayoutHeader"" />
</head>
<body>
    <!-- Layout wrapper -->
    <div class=""layout-wrapper layout-content-navbar"">
        <div class=""layout-container"">

            @await Component.InvokeAsync(""SideMenu"")

            <!-- Layout container -->
            <div class=""layout-page"">

                <!-- Navbar -->
                <nav class=""layout-navbar container-xxl navbar navbar-expand-xl navbar-detached align-items-center bg-navbar-theme"" id=""layout-navbar"">
                    <div class=""layout-menu-toggle navbar-nav align-items-xl-center me-3 me-xl-0 d-xl-none"">
                        <a class=""nav-item nav-link px-0 me-xl-4"" href=""javascript:void(0)"">
                            <i class=""fa-solid fa-bars""></i>
                        </a>
                    </div>

                    <div class=""navbar-nav-right d-flex align-items-center"" id=""navbar-collapse"">
                        <!-- Search -->
                        <div class=""navbar-nav align-items-center"">
                            <div class=""nav-item d-flex align-items-center"">
                                <i class=""bx bx-search fs-4 lh-0""></i>
                                <input type=""text"" class=""form-control border-0 shadow-none"" placeholder=""Search...""
                                       aria-label=""Search..."" />
                            </div>
                        </div>
                        <!-- /Search -->

                        <ul class=""navbar-nav flex-row align-items-center ms-auto"">

                            <!-- Light Mode -->
                            <li class=""nav-item dropdown-style-switcher dropdown me-2 me-xl-0"">
                                <a class=""nav-link dropdown-toggle hide-arrow"" href=""javascript:void(0);"" data-bs-toggle=""dropdown"">
                                    <i class=""fa-solid fa-sun fa-xl text-warning""></i>
                                </a>
                                <ul class=""dropdown-menu dropdown-menu-end dropdown-styles"">
                                    <li>
                                        <a class=""dropdown-item  "" asp-controller=""UI"" asp-action=""SetLightMode"" asp-route-mode=""light""
                                           asp-route-returnUrl=""@currentUrl"">
                                            <span><i class=""fa-solid fa-lightbulb fa-lg me-4""></i> Light</span>
                                        </a>
                                    </li>
                                    <li>
                                        <a class=""dropdown-item "" asp-controller=""UI"" asp-action=""SetLightMode"" asp-route-mode=""dark""
                                           asp-route-returnUrl=""@currentUrl"">
                                            <span><i class=""fa-solid fa-moon fa-lg me-4""></i> Dark</span>
                                        </a>
                                    </li>
                                    <li>
                                        <a class=""dropdown-item "" asp-controller=""UI"" asp-action=""SetLightMode"" asp-route-mode=""system""
                                           asp-route-returnUrl=""@currentUrl"">
                                            <span><i class=""fa-solid fa-display me-2 fa-lg me-3""></i> System</span>
                                        </a>
                                    </li>
                                </ul>
                            </li>

                            <!-- ShortCuts -->
                            <li class=""nav-item dropdown-shortcuts navbar-dropdown dropdown me-2 me-xl-0"">
                                <a class=""nav-link dropdown-toggle hide-arrow"" href=""javascript:void(0);"" data-bs-toggle=""dropdown""
                                   data-bs-auto-close=""outside"" aria-expanded=""false"">
                                    <i class='fa-brands fa-windows fa-xl'></i>
                                </a>
                                <div class=""dropdown-menu dropdown-menu-end p-0"">
                                    <div class=""dropdown-menu-header border-bottom"">
                                        <div class=""dropdown-header d-flex align-items-center py-3"">
                                            <h6 class=""mb-0 me-auto"">Shortcuts</h6>
                                            <span id=""dropdown-shortcuts-add"" class=""dropdown-shortcuts-add py-2"" data-bs-toggle=""tooltip"" role=""button""
                                               data-bs-placement=""top"" title=""Add shortcuts""><i class=""bx bx-plus-circle text-heading""></i></span>
                                        </div>
                                    </div>
                                    <div id=""dropdownshortcutslist"" class=""dropdown-shortcuts-list scrollable-container"">
                                        <!-- ShortCuts Coming Here -->
                                    </div>
                                </div>
                            </li>
                            <!-- Quick links -->
                            <!-- User -->
                            <li class=""nav-item navbar-dropdown dropdown-user dropdown"">
                                <a class=""nav-link dropdown-toggle hide-arrow"" href=""javascript:void(0);"" data-bs-toggle=""dropdown"">
                                    <div class=""avatar avatar-online"">
                                        <img src=""../assets/img/avatars/1.png"" alt class=""w-px-40 h-auto rounded-circle"" />
                                    </div>
                                </a>
                                <ul class=""dropdown-menu dropdown-menu-end"">
                                    <li>
                                        <a class=""dropdown-item"" href=""#"">
                                            <div class=""d-flex"">
                                                <div class=""flex-shrink-0 me-3"">
                                                    <div class=""avatar avatar-online"">
                                                        <img src=""../assets/img/avatars/1.png"" alt class=""w-px-40 h-auto rounded-circle"" />
                                                    </div>
                                                </div>
                                                <div class=""flex-grow-1"">
                                                    <span class=""fw-semibold d-block"">John Doe</span>
                                                    <small class=""text-muted"">Admin</small>
                                                </div>
                                            </div>
                                        </a>
                                    </li>
                                    <li>
                                        <div class=""dropdown-divider""></div>
                                    </li>
                                    <li>
                                        <a class=""dropdown-item"" href=""#"">
                                            <i class=""bx bx-user me-2""></i>
                                            <span class=""align-middle"">My Profile</span>
                                        </a>
                                    </li>
                                    <li>
                                        <a class=""dropdown-item"" href=""#"">
                                            <i class=""bx bx-cog me-2""></i>
                                            <span class=""align-middle"">Settings</span>
                                        </a>
                                    </li>
                                    <li>
                                        <a class=""dropdown-item"" href=""#"">
                                            <span class=""d-flex align-items-center align-middle"">
                                                <i class=""flex-shrink-0 bx bx-credit-card me-2""></i>
                                                <span class=""flex-grow-1 align-middle"">Billing</span>
                                                <span class=""flex-shrink-0 badge badge-center rounded-pill bg-danger w-px-20 h-px-20"">4</span>
                                            </span>
                                        </a>
                                    </li>
                                    <li>
                                        <div class=""dropdown-divider""></div>
                                    </li>
                                    <li>
                                        <a asp-controller=""Account"" asp-action=""LogOut"" class=""dropdown-item"">
                                            <i class=""bx bx-power-off me-2""></i>
                                            <span class=""align-middle"">Log Out</span>
                                        </a>
                                    </li>
                                </ul>
                            </li>
                            <!--/ User -->
                        </ul>

                    </div>
                </nav>
                <!-- / Navbar -->
                <!-- Content wrapper -->
                <div class=""content-wrapper"">
                    <!-- Content -->

                    <div class=""container-xxl flex-grow-1 container-p-y"">
                        @RenderBody()
                    </div>
                    <!-- / Content -->
                    <!-- Footer -->
                    <footer class=""content-footer footer bg-light"">
                        <div class=""container-fluid d-flex flex-md-row flex-column justify-content-between align-items-md-center gap-1 container-p-x py-4"">
                            <div>
                                <a href=""/"" target=""_blank"" class=""footer-brand fw-bold"">My App</a> ©
                            </div>
                            <div>
                                <a href=""/"" class=""footer-link me-6"" target=""_blank"">License</a>
                            </div>
                        </div>
                    </footer>
                    <!-- / Footer -->

                    <div class=""content-backdrop fade""></div>
                </div>
                <!-- Content wrapper -->
            </div>
            <!-- / Layout page -->
        </div>

        <!-- Overlay -->
        <div class=""layout-overlay layout-menu-toggle""></div>
    </div>
    <partial name=""_LayoutScripts"" />

    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";

        string code_LayoutBase = @"@{
    var currentUrl = Context.GetUrl();
    var lightMode = Context.GetLightMode();
}
<!DOCTYPE html>
<html class=""@(lightMode == ""light"" ? ""light-style"" : ""dark-style"") layout-menu-fixed layout-menu-expanded overflow-x-hidden""
      dir=""ltr""
      data-theme=""theme-default""
      data-style=""light""
      lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""]</title>
    <link rel=""icon"" type=""image/x-icon"" href=""~/assets/img/favicon/favicon.ico"" />

    <partial name=""_LayoutHeader"" />
</head>
<body>
    <!-- Layout wrapper -->
    <div class=""layout-wrapper layout-content-navbar"">
        <div class=""layout-container""> 
            <!-- Layout container --> 
                <!-- Content wrapper -->
                <div class=""content-wrapper"">
                    <!-- Content -->

                    <div class=""container-sm flex-grow-1 container-p-y"">
                        @RenderBody()
                    </div>
                    <!-- / Content --> 
                </div>
                <!-- Content wrapper --> 
            <!-- / Layout page -->
        </div> 
    </div>
    <partial name=""_LayoutScripts"" />

    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>";

        string code_LayoutHeader = @"
@{
    var lightMode = Context.GetLightMode();
}

<!-- LIBS -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/boxicons/2.1.4/css/boxicons.min.css"" integrity=""sha512-cn16Qw8mzTBKpu08X0fwhTSv02kK/FojjNLz0bwp2xJ4H+yalwzXKFw/5cLzuBZCxGWIA+95X4skzvo8STNtSg=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer"" />
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.0/css/all.min.css"" integrity=""sha512-DxV+EoADOkOygM4IR9yXP8Sb2qwgidEmeqAEmDKIOfPRQZOWbXCzLC6vjbZyy0vPisbH2SyW27+ddLVCN+OMzQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer"" />
<link rel=""stylesheet"" href=""~/lib/bootstrap/dist/css/bootstrap.min.css"" />
<link rel=""stylesheet"" href=""~/lib/perfect-scrollbar/perfect-scrollbar.min.css"" />
<link rel=""stylesheet"" href=""~/lib/apex-charts/apex-charts.css"" />
<link rel=""stylesheet"" href=""~/lib/select2/select2.css"" />
<link rel=""stylesheet"" href=""~/lib/tagify/tagify.css"" />
<link rel=""stylesheet"" href=""~/lib/typeahead-js/typeahead.css"" />
<link rel=""stylesheet"" href=""~/lib/bs-stepper/bs-stepper.css"" />
<link rel=""stylesheet"" href=""~/lib/flatpickr/flatpickr.css"" />
<link rel=""stylesheet"" href=""~/lib/pickr/pickr-themes.css"" />
<link rel=""stylesheet"" href=""~/lib/jquery-timepicker/jquery-timepicker.css"" />
<link rel=""stylesheet"" href=""~/lib/simple-notify/simple-notify.min.css"" />
<link rel=""stylesheet"" href=""~/lib/pace/pace-theme-minimal.css"" />
<link rel=""stylesheet"" href=""~/lib/bootstrap-daterangepicker/bootstrap-daterangepicker.css"" />
<link rel=""stylesheet"" href=""~/lib/bootstrap-datepicker/bootstrap-datepicker.css"" />

<link rel=""stylesheet"" href=""~/lib/datatables-bs5/datatables.bootstrap5.css"" />
<link rel=""stylesheet"" href=""~/lib/datatables-buttons-bs5/buttons.bootstrap5.css"" />
<link rel=""stylesheet"" href=""~/lib/datatables-responsive-bs5/responsive.bootstrap5.css"" />
<!-- LIBS -->
@if (lightMode == ""dark"")
{
    <link rel=""stylesheet"" href=""~/css/core-dark.css"" class=""template-customizer-core-css"" />
    <link rel=""stylesheet"" href=""~/css/theme-default-dark.css"" class=""template-customizer-theme-css"" />
}
else
{
    <link rel=""stylesheet"" href=""~/css/core.css"" class=""template-customizer-core-css"" />
    <link rel=""stylesheet"" href=""~/css/theme-default.css"" class=""template-customizer-theme-css"" />
}
<link rel=""stylesheet"" href=""~/css/site.css"" asp-append-version=""true"" />

<script src=""~/js/config.js""></script>
<script src=""~/js/helpers.js""></script>";

        string code_LayoutScripts = @"
<script src=""~/lib/jquery/dist/jquery.min.js""></script>
<script src=""~/lib/jquery-validation/dist/jquery.validate.min.js""></script>
<script src=""~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js""></script>
<script src=""~/lib/bootstrap/dist/js/bootstrap.bundle.min.js""></script>

<!-- LIBS -->
<script src=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.0/js/all.min.js"" integrity=""sha512-gBYquPLlR76UWqCwD06/xwal4so02RjIR0oyG1TIhSGwmBTRrIkQbaPehPF8iwuY9jFikDHMGEelt0DtY7jtvQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer""></script>
<script src=""~/lib/popper/popper.js""></script>
<script src=""~/lib/perfect-scrollbar/perfect-scrollbar.min.js""></script>
<script src=""~/lib/highlight/highlight.js""></script>
<script src=""~/lib/clipboard/clipboard.js""></script>
<script src=""~/lib/@@algolia/autocomplete-js.js""></script>
<script src=""~/lib/hammer/hammer.js""></script>
<script src=""~/lib/i18n/i18n.js""></script>
<script src=""~/lib/bloodhound/bloodhound.js""></script>
<script src=""~/lib/apex-charts/apexcharts.js""></script>
<script src=""~/lib/select2/select2.js""></script>
<script src=""~/lib/tagify/tagify.js""></script>
<script src=""~/lib/typeahead-js/typeahead.js""></script>
<script src=""~/lib/bs-stepper/bs-stepper.js""></script>
<script src=""~/lib/bootstrap-select/bootstrap-select.js""></script>
<script src=""~/lib/moment/moment.js""></script>
<script src=""~/lib/flatpickr/flatpickr.js""></script>
<script src=""~/lib/pickr/pickr.js""></script>
<script src=""~/lib/bootstrap-daterangepicker/bootstrap-daterangepicker.js""></script>
<script src=""~/lib/jquery-timepicker/jquery-timepicker.js""></script>
<script src=""~/lib/simple-notify/simple-notify.min.js""></script>
<script src=""~/lib/pace/pace.min.js""></script>

<script src=""~/lib/bootstrap-datepicker-1.9.0/js/bootstrap-datepicker.min.js""></script>
<script src=""~/lib/bootstrap-datepicker-1.9.0/locales/bootstrap-datepicker.tr.min.js""></script>
<script src=""~/lib/datatables-bs5/datatables-bootstrap5.js""></script>
<!-- LIBS -->

<script src=""~/js/menu.js""></script>

<script src=""~/js/utils/requestManager.js""></script>
<script src=""~/js/utils/alertManager.js""></script>
<script src=""~/js/utils/datatableManager.js""></script>
<script src=""~/js/utils/modalManager.js""></script>
<script src=""~/js/UIManager.js""></script>

<script src=""~/js/main.js""></script>

<script>
	window.paceOptions = {
		ajax: {
			trackMethods: ['GET', 'POST', 'DELETE', 'PACH'],
			ignoreURLs: [
				""/heartbeat"",
				""/keepalive"",
				""signalr"",
				""__browserLink"",
				""browserLinkSignalR""
			],
			trackWebSockets: false,
		},
		document: true,
		eventLag: false,
		restartOnRequestAfter: false
	};
	// jquery pace.js handle
	$(document).ajaxStart(function () { Pace.restart(); });

	// fetch pace.js handle
	const _fetch = window.fetch;
	window.fetch = function (...args) {
		Pace.restart();
		return _fetch.apply(this, args);
	};
</script>";

        string code_ViewImports = @"
﻿@using WebUI
@using WebUI.Models
@using WebUI.Utils.Extensions
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers";

        string viewsPath = Path.Combine(solutionPath, "WebUI", "Views");
        string sharedViewsSharedPath = Path.Combine(solutionPath, "WebUI", "Views", "Shared");

        results.Add(AddFileByExt(sharedViewsSharedPath, "_Layout.cshtml", code_Layout)); 
        results.Add(AddFileByExt(sharedViewsSharedPath, "_LayoutBase.cshtml", code_LayoutBase)); 
        results.Add(AddFileByExt(sharedViewsSharedPath, "_LayoutHeader.cshtml", code_LayoutHeader));
        results.Add(AddFileByExt(sharedViewsSharedPath, "_LayoutScripts.cshtml", code_LayoutScripts));
        results.Add(AddFileByExt(viewsPath, "_ViewImports.cshtml", code_ViewImports));
        #endregion

        #region SideMenu
        string code_SideMenuDefault = @"@using WebUI.Models.UI
@model List<MenuItem>
@{
	string _path_ = Context.GetPath();
	string _basePath_ = Context.GetBasePath();
} 


<aside id=""layout-menu"" class=""layout-menu menu-vertical menu bg-menu-theme"">
	<div class=""app-brand demo"">
		<a href=""/"" class=""app-brand-link"">
			<span id=""side_menu_logo"" class=""app-brand-logo demo"">
				<svg width=""25""
					 viewBox=""0 0 25 42""
					 version=""1.1""
					 xmlns=""http://www.w3.org/2000/svg""
					 xmlns:xlink=""http://www.w3.org/1999/xlink"">
					<defs>
						<path d=""M13.7918663,0.358365126 L3.39788168,7.44174259 C0.566865006,9.69408886 -0.379795268,12.4788597 0.557900856,15.7960551 C0.68998853,16.2305145 1.09562888,17.7872135 3.12357076,19.2293357 C3.8146334,19.7207684 5.32369333,20.3834223 7.65075054,21.2172976 L7.59773219,21.2525164 L2.63468769,24.5493413 C0.445452254,26.3002124 0.0884951797,28.5083815 1.56381646,31.1738486 C2.83770406,32.8170431 5.20850219,33.2640127 7.09180128,32.5391577 C8.347334,32.0559211 11.4559176,30.0011079 16.4175519,26.3747182 C18.0338572,24.4997857 18.6973423,22.4544883 18.4080071,20.2388261 C17.963753,17.5346866 16.1776345,15.5799961 13.0496516,14.3747546 L10.9194936,13.4715819 L18.6192054,7.984237 L13.7918663,0.358365126 Z""
							  id=""path-1""></path>
						<path d=""M5.47320593,6.00457225 C4.05321814,8.216144 4.36334763,10.0722806 6.40359441,11.5729822 C8.61520715,12.571656 10.0999176,13.2171421 10.8577257,13.5094407 L15.5088241,14.433041 L18.6192054,7.984237 C15.5364148,3.11535317 13.9273018,0.573395879 13.7918663,0.358365126 C13.5790555,0.511491653 10.8061687,2.3935607 5.47320593,6.00457225 Z""
							  id=""path-3""></path>
						<path d=""M7.50063644,21.2294429 L12.3234468,23.3159332 C14.1688022,24.7579751 14.397098,26.4880487 13.008334,28.506154 C11.6195701,30.5242593 10.3099883,31.790241 9.07958868,32.3040991 C5.78142938,33.4346997 4.13234973,34 4.13234973,34 C4.13234973,34 2.75489982,33.0538207 2.37032616e-14,31.1614621 C-0.55822714,27.8186216 -0.55822714,26.0572515 -4.05231404e-15,25.8773518 C0.83734071,25.6075023 2.77988457,22.8248993 3.3049379,22.52991 C3.65497346,22.3332504 5.05353963,21.8997614 7.50063644,21.2294429 Z""
							  id=""path-4""></path>
						<path d=""M20.6,7.13333333 L25.6,13.8 C26.2627417,14.6836556 26.0836556,15.9372583 25.2,16.6 C24.8538077,16.8596443 24.4327404,17 24,17 L14,17 C12.8954305,17 12,16.1045695 12,15 C12,14.5672596 12.1403557,14.1461923 12.4,13.8 L17.4,7.13333333 C18.0627417,6.24967773 19.3163444,6.07059163 20.2,6.73333333 C20.3516113,6.84704183 20.4862915,6.981722 20.6,7.13333333 Z""
							  id=""path-5""></path>
					</defs>
					<g id=""g-app-brand"" stroke=""none"" stroke-width=""1"" fill=""none"" fill-rule=""evenodd"">
						<g id=""Brand-Logo"" transform=""translate(-27.000000, -15.000000)"">
							<g id=""Icon"" transform=""translate(27.000000, 15.000000)"">
								<g id=""Mask"" transform=""translate(0.000000, 8.000000)"">
									<mask id=""mask-2"" fill=""white"">
										<use xlink:href=""#path-1""></use>
									</mask>
									<use fill=""#248ef0"" xlink:href=""#path-1""></use>
									<g id=""Path-3"" mask=""url(#mask-2)"">
										<use fill=""#248ef0"" xlink:href=""#path-3""></use>
										<use fill-opacity=""0.2"" fill=""#FFFFFF"" xlink:href=""#path-3""></use>
									</g>
									<g id=""Path-4"" mask=""url(#mask-2)"">
										<use fill=""#248ef0"" xlink:href=""#path-4""></use>
										<use fill-opacity=""0.2"" fill=""#FFFFFF"" xlink:href=""#path-4""></use>
									</g>
								</g>
								<g id=""Triangle""
								   transform=""translate(19.000000, 11.000000) rotate(-300.000000) translate(-19.000000, -11.000000) "">
									<use fill=""#248ef0"" xlink:href=""#path-5""></use>
									<use fill-opacity=""0.2"" fill=""#FFFFFF"" xlink:href=""#path-5""></use>
								</g>
							</g>
						</g>
					</g>
				</svg>
			</span>
			<span class=""app-brand-text demo menu-text fw-bolder ms-2"">Sneat</span>
		</a>

		<span role=""button"" class=""layout-menu-toggle menu-link text-large ms-auto d-flex align-items-center justify-content-center"">
			<i class=""bx bx-chevron-left bx-sm d-flex align-items-center justify-content-center""></i>
		</span>
	</div>

	<hr />

	<div class=""menu-inner-shadow""></div>

	<ul class=""menu-inner py-1"">
		<!-- Dashboard -->
		@* <li class=""menu-item active"">
		<a href=""index.html"" class=""menu-link"">
		<i class=""menu-icon tf-icons bx bx-home-circle""></i>
		<div data-i18n=""Analytics"">Dashboard</div>
		</a>
		</li> *@

		@foreach (var item in Model)
		{
			if (!string.IsNullOrEmpty(item.GroupName))
			{
				<li class=""menu-header small text-uppercase"">
					<span class=""menu-header-text"">
						@item.GroupName
					</span>
				</li>
			}

			bool isThereSubs = false;
			bool isActive = false;
			if (item.Type == 0)
			{
				isThereSubs = item.SubMenuItems != null && item.SubMenuItems.Any();
				isActive = isThereSubs && item.SubMenuItems!.Any(f => f.Path == _basePath_);

				<li class=""menu-item @(isActive ? ""active open"" : string.Empty)"">
					<span role=""button"" class=""menu-link @(isActive ? ""active"" : string.Empty) @(isThereSubs ? ""menu-toggle"" : string.Empty)"" data-icon=""@item.Icon"">
						<i class=""menu-icon @item.Icon""></i>
						<div class=""page-name"">@item.Title</div>
					</span>
					@if (isThereSubs)
					{
						<partial name=""./_subMenu.cshtml"" model=""@item.SubMenuItems"" />
					}
				</li>
			}
			else if (item.Type == 1)
			{
				isActive = item.Path == _basePath_;

				<li class=""menu-item @(isActive ? ""active open"" : string.Empty)"">
					<a href=""@item.Path"" class=""menu-link @(isActive ? ""active"" : string.Empty) @(isThereSubs ? ""menu-toggle"" : string.Empty)"" data-icon=""@item.Icon"">
						<i class=""menu-icon @item.Icon""></i>
						<div class=""page-name"">@item.Title</div>
					</a>
					@if (isThereSubs)
					{
						<partial name=""./_subMenu.cshtml"" model=""@item.SubMenuItems"" />
					}
				</li>
			}
		}
	</ul>
</aside>";
        
        string code_SideMenuSubMenu = @"@using WebUI.Models.UI
@model List<MenuItem>
@{
	string _path_ = Context.GetPath();
	string _basePath_ = Context.GetBasePath();
}

<ul class=""menu-sub"">
	@foreach (var item in Model)
	{
		if (!string.IsNullOrEmpty(item.GroupName))
		{
			<li class=""menu-header small text-uppercase"">
				<span class=""menu-header-text"">
					@item.GroupName
				</span>
			</li>
		}

		bool isThereSubs = false;
		bool isActive = false;
		if (item.Type == 0)
		{
			isThereSubs = item.SubMenuItems != null && item.SubMenuItems.Any();
			isActive = isThereSubs && item.SubMenuItems!.Any(f => f.Path == _basePath_);

			<li class=""menu-item @(isActive ? ""active open"" : string.Empty)"">
				<span role=""button"" class=""menu-link @(isActive ? ""active"" : string.Empty) @(isThereSubs ? ""menu-toggle"" : string.Empty)"" data-icon=""@item.Icon"">
					<i class=""menu-icon @item.Icon""></i>
					<div class=""page-name"">@item.Title</div>
				</span>
				@if (isThereSubs)
				{
					<partial name=""./_subMenu.cshtml"" model=""@item.SubMenuItems"" />
				}
			</li>
		}
		else if (item.Type == 1)
		{
			isActive = item.Path == _basePath_;

			<li class=""menu-item @(isActive ? ""active open"" : string.Empty)"">
				<a href=""@item.Path"" class=""menu-link @(isActive ? ""active"" : string.Empty) @(isThereSubs ? ""menu-toggle"" : string.Empty)"" data-icon=""@item.Icon"">
					<i class=""menu-icon @item.Icon""></i>
					<div class=""page-name"">@item.Title</div>
				</a>
				@if (isThereSubs)
				{
					<partial name=""./_subMenu.cshtml"" model=""@item.SubMenuItems"" />
				}
			</li>
		}
	}
</ul>";

        string sideMenuViewsPath = Path.Combine(solutionPath, "WebUI", "Views", "Shared", "Components", "SideMenu");
        results.Add(AddFileByExt(sideMenuViewsPath, "Default.cshtml", code_SideMenuDefault));
        results.Add(AddFileByExt(sideMenuViewsPath, "_subMenu.cshtml", code_SideMenuSubMenu));
        #endregion

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
                return $"OK: File {fileName} added to WebUI project.";
            }
            else
            {
                return $"INFO: File {fileName} already exists in WebUI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}) to WebUI project. \n Details:{ex.Message}");
        }
    }

    private string AddFileByExt(string folderPath, string fileName, string code)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"{fileName}");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File {fileName} added to WebUI project.";
            }
            else
            {
                return $"INFO: File {fileName} already exists in WebUI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}) to WebUI project. \n Details:{ex.Message}");
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
                return $"OK: File {fileName} removed from WebUI project.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist in WebUI project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) from WebUI project. \n Details: {ex.Message}");
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
        sb.AppendLine(@"
// ------- CORS -------
builder.Services.AddCors(options =>
{
    options.AddPolicy(""policy_cors"", builder =>
    {
        builder
            .AllowAnyOrigin()
            //.WithOrigins(""https://www.frontend.com"")
            //.AllowCredentials() // AllowAnyOrigin and AllowCredentials cannot using together use with WithOrigins option 
            .WithHeaders(""Content-Type"", ""Authorization"")
            .AllowAnyMethod()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
// ------- CORS -------
");
    }

    private void AddRateLimiter(ref StringBuilder sb)
    {
        sb.AppendLine(@"
// ------- Rate Limiter -------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter(policyName: ""policy_rate_limiter"", slidingOptions =>
    {
        slidingOptions.PermitLimit = 15;
        slidingOptions.Window = TimeSpan.FromSeconds(3);
        slidingOptions.SegmentsPerWindow = 4;
        slidingOptions.QueueLimit = 5;
        slidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
// ------- Rate Limiter -------
");
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
        sb.AppendLine(@"
// ------- Layer Registrations -------
builder.Services.AddModelServices();
builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddDataAccessServices(builder.Configuration);
builder.Services.AddBusinessServices(builder.Configuration);
// ------- Layer Registrations -------
");
    }

    private void AddAutofacModules(ref StringBuilder sb)
    {
        sb.AppendLine(@"
// ------- Autofac Modules -------
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new Core.AutofacModule());
        builder.RegisterModule(new DataAccess.AutofacModule());
        builder.RegisterModule(new Business.AutofacModule());
    });
// ------- Autofac Modules -------
");
    }

    private void AddIdentityImplemantation(ref StringBuilder sb, string identityUserType, string identityRoleType)
    {
        sb.AppendLine("");
        sb.AppendLine("// ------- IDENTITY -------");
        sb.AppendLine("builder.Services.AddSingleton<TokenSettings>(new TokenSettings());");
        sb.AppendLine();
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
        sb.AppendLine();
        sb.AppendLine("builder.Services.AddAuthorization();");
        sb.AppendLine("// ------- IDENTITY -------");
        sb.AppendLine("");
    }

    private void AddCookieOptions(ref StringBuilder sb)
    {
        sb.AppendLine(@"
// ------- Cookie Options -------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = ""/Error/Forbidden"";
    options.LogoutPath = ""/Account/Logout"";
    options.LoginPath = ""/Account/Login"";
    options.Cookie = new()
    {
        Name = ""IdentityCookie"",
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        SecurePolicy = CookieSecurePolicy.Always
    };
});
// ------- Cookie Options -------
");
    }

    private void AddActionFilters(ref StringBuilder sb)
    {
        sb.AppendLine(@"
// ------- Action Filters -------
builder.Services.AddScoped(typeof(WebUI.Utils.ActionFilters.ValidationFilter<>));
// ------- Action Filters -------
");
    }
    #endregion
}
