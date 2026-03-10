using GeneratorWPF.CodeGenerators.NLayer.API;
using GeneratorWPF.CodeGenerators.NLayer.Base;
using GeneratorWPF.CodeGenerators.NLayer.Business;
using GeneratorWPF.CodeGenerators.NLayer.Core;
using GeneratorWPF.CodeGenerators.NLayer.DataAccess;
using GeneratorWPF.CodeGenerators.NLayer.Model;
using GeneratorWPF.CodeGenerators.NLayer.WebUI;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using System.IO;

namespace GeneratorWPF.CodeGenerators.NLayer;

public class NLayerGeneratorService
{
    private readonly AppSettingsRepository _appSettingsRepository;
    private readonly AppSetting _appSetting;
    public NLayerGeneratorService()
    {
        _appSettingsRepository = new AppSettingsRepository();
        _appSetting = _appSettingsRepository.Get(f => f.Id == 1);
    }

    public bool GenerateSolution(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName) || string.IsNullOrEmpty(_appSetting.ProjectName))
                throw new Exception("App Settings Not Completted To Generate!");

            var NLayerBaseService = new NLayerGeneratorBase(_appSetting);

            log(NLayerBaseService.CreateSolution());

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public bool GenerateCoreLayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName) || string.IsNullOrEmpty(_appSetting.ProjectName))
                throw new Exception("App Settings Not Completted To Generate!");

            var NLayerCoreService = new NLayerCoreGenerator(_appSetting);

            // 1. Create Core Class Library if not exists
            log(NLayerCoreService.CreateClassLibrearyProject(_appSetting.CoreLayerProjectName));

            // 2. Add Packages
            log(NLayerCoreService.AddPackage("AutoMapper --version 14.0.0", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("FluentValidation --version 12.1.1", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("FluentValidation.DependencyInjectionExtensions --version 12.1.1", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("Microsoft.EntityFrameworkCore --version 10.0.3", _appSetting.CoreLayerProjectName)); 
            log(NLayerCoreService.AddPackage("Newtonsoft.Json --version 13.0.4", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("Serilog.AspNetCore --version 10.0.0", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("Serilog.Sinks.Async --version 2.1.0", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("Serilog.Sinks.File --version 7.0.0", _appSetting.CoreLayerProjectName));
            log(NLayerCoreService.AddPackage("System.Linq.Dynamic.Core --version 1.7.1", _appSetting.CoreLayerProjectName));

            log(NLayerCoreService.Restore(_appSetting.CoreLayerProjectName));

            // 3. Files
            log(NLayerCoreService.GenerateStaticFiles("Core", _appSetting.CoreLayerProjectName));
            
            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public bool GenerateModelLayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName))
                throw new Exception("App Settings Not Completted To Generate!");

            var NLayerModelService = new NLayerModelGenerator(_appSetting);
             
            // 1. Create Core Class Library if not exists
            log(NLayerModelService.CreateClassLibrearyProject(_appSetting.ModelLayerProjectName, referances: [$"../{_appSetting.CoreLayerProjectName}/{_appSetting.CoreLayerProjectName}.csproj"]));

            // 2. Files
            log(NLayerModelService.GenerateStaticFiles("Model", _appSetting.ModelLayerProjectName));

            // 3. Auth
            if (_appSetting.IsThereIdentiy)
            {
                log(NLayerModelService.GenerateAuthModels());
            }

            // 4. Dtos
            log(NLayerModelService.GenerateDtos());

            // 5. Entities
            log(NLayerModelService.GenerateEntities());

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public bool GenerateDataAccessLayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName))
                throw new Exception("App Settings Not Completted To Generate!");

            var nLayerDataAccessService = new NLayerDataAccessGenerator(_appSetting);

            string solutionPath = Path.Combine(_appSetting.Path, _appSetting.SolutionName);

            // 1. Create Core Class Library if not exists
            log(nLayerDataAccessService.CreateProject(solutionPath, _appSetting.SolutionName));

            // 2. Repository Base
            log(nLayerDataAccessService.GenerateRepositoryBase(solutionPath));

            // 3. Interceptors
            log(nLayerDataAccessService.GenerateInterceptors(solutionPath));

            // 4. Servises
            log(nLayerDataAccessService.GenerateServices(solutionPath));

            // 5. UOW
            log(nLayerDataAccessService.GenerateUOW(solutionPath));

            // 6. Context Fiel
            log(nLayerDataAccessService.GenerateContext(solutionPath));

            // 7. Service Registrations
            log(nLayerDataAccessService.GenerateServiceRegistrations(solutionPath));

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public bool GenerateBusinessLayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName))
                throw new Exception("App Settings Not Completted To Generate!");

            var nLayerBusinessService = new NLayerBusinessGenerator(_appSetting);

            string solutionPath = Path.Combine(_appSetting.Path, _appSetting.SolutionName);

            // 1. Create Core Class Library if not exists
            log(nLayerBusinessService.CreateProject(solutionPath, _appSetting.SolutionName));

            // 2. Service Base
            log(nLayerBusinessService.GenerateServiceBase(solutionPath));

            // 3. Utils
            log(nLayerBusinessService.GenerateUtils(solutionPath));

            // 4. Mappings
            log(nLayerBusinessService.GenerateMappings(solutionPath));

            // 5. Concretes
            log(nLayerBusinessService.GeneraterService(solutionPath));

            // 6. Service Registrations
            log(nLayerBusinessService.GenerateServiceRegistrations(solutionPath));

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    public bool GenerateAPILayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName))
                throw new Exception("App Settings Not Completted To Generate!");

            var nLayerAPIService = new NLayerAPIService(_appSetting);

            string solutionPath = Path.Combine(_appSetting.Path, _appSetting.SolutionName);

            // 1. Create Core Class Library if not exists
            log(nLayerAPIService.CreateProject(solutionPath, _appSetting.SolutionName));

            // 2. Add Packages
            log(nLayerAPIService.AddPackage(solutionPath, "Microsoft.AspNetCore.Authentication.JwtBearer"));
            log(nLayerAPIService.AddPackage(solutionPath, "Microsoft.AspNetCore.OpenApi"));
            log(nLayerAPIService.AddPackage(solutionPath, "Microsoft.EntityFrameworkCore.Design"));
            log(nLayerAPIService.AddPackage(solutionPath, "Scalar.AspNetCore"));

            // 3. Exception Handler
            log(nLayerAPIService.GenerateExceptionHandler(solutionPath));

            // 4. Scalar Security Scheme Transformer
            log(nLayerAPIService.GenerateScalarSecuritySchemeTransformer(solutionPath));

            // 5. Program.cs
            log(nLayerAPIService.GenerateProgramCs(solutionPath));

            // 6. Mappings
            log(nLayerAPIService.GenerateAppSettings(solutionPath));

            // 7. Controllers
            log(nLayerAPIService.GenerateControllers(solutionPath));

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }


    public bool GenerateWebUIILayer(Action<string> log)
    {
        try
        {
            if (_appSetting == null || string.IsNullOrEmpty(_appSetting.Path) || string.IsNullOrEmpty(_appSetting.SolutionName))
                throw new Exception("App Settings Not Completted To Generate!");

            var nLayerWebUIService = new NLayerWebUIGenerator(_appSetting);

            string solutionPath = Path.Combine(_appSetting.Path, _appSetting.SolutionName);

            // 1. Create Project if not exists
            log(nLayerWebUIService.CreateProject(solutionPath, _appSetting.SolutionName));

            // 2. Add Packages
            log(nLayerWebUIService.AddPackage(solutionPath, "FluentValidation.AspNetCore"));
            log(nLayerWebUIService.AddPackage(solutionPath, "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation"));
            log(nLayerWebUIService.AddPackage(solutionPath, "Microsoft.VisualStudio.Web.CodeGeneration.Design"));
            log(nLayerWebUIService.AddPackage(solutionPath, "Microsoft.EntityFrameworkCore.Design"));

            // 3. Utils
            log(nLayerWebUIService.GenerateUtils(solutionPath));

            // 4. Exception Handler
            log(nLayerWebUIService.GenerateExceptionHandler(solutionPath));

            // 5. Side Menu ViewComponent
            log(nLayerWebUIService.GenerateSideMenuViewComponent(solutionPath));

            // 6. wwwroot
            log(nLayerWebUIService.Generate_wwwroot(solutionPath));

            // 7. ViewModels
            log(nLayerWebUIService.GenerateViewModels(solutionPath));

            // 8. Program.cs
            log(nLayerWebUIService.GenerateProgramCs(solutionPath));

            // 9. AppSettings.json
            log(nLayerWebUIService.GenerateAppSettings(solutionPath));

            // 10. Controllers
            log(nLayerWebUIService.GenerateControllers(solutionPath));

            // 11. Views
            log(nLayerWebUIService.GenerateViews(solutionPath));

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }
}
