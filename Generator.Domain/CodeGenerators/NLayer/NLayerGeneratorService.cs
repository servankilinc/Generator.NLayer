using Generator.Domain.CodeGenerators.NLayer.API;
using Generator.Domain.CodeGenerators.NLayer.Base;
using Generator.Domain.CodeGenerators.NLayer.Business;
using Generator.Domain.CodeGenerators.NLayer.Core;
using Generator.Domain.CodeGenerators.NLayer.DataAccess;
using Generator.Domain.CodeGenerators.NLayer.Model;
using Generator.Domain.CodeGenerators.NLayer.WebUI;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository;
using System.Security.Cryptography;

namespace Generator.Domain.CodeGenerators.NLayer;

public class NLayerGeneratorService
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly AppSettingsRepository _appSettingsRepository;
    private readonly AppSetting _appSetting;
    public NLayerGeneratorService()
    {
        _entityRepository = new();
        _fieldRepository = new();
        _appSettingsRepository = new();
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
            log(NLayerCoreService.AddPackage("Microsoft.EntityFrameworkCore --version 10.0.4", _appSetting.CoreLayerProjectName));
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
            if (_appSetting.IsThereIdentity)
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

            // 1. Create Core Class Library if not exists
            log(nLayerDataAccessService.CreateClassLibrearyProject(_appSetting.DataAccessLayerProjectName, referances: [$"../{_appSetting.ModelLayerProjectName}/{_appSetting.ModelLayerProjectName}.csproj"]));

            // 2. Add Packages
            log(nLayerDataAccessService.AddPackage("Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.4", _appSetting.DataAccessLayerProjectName));
            log(nLayerDataAccessService.AddPackage("Microsoft.EntityFrameworkCore.Design --version 10.0.4", _appSetting.DataAccessLayerProjectName));
            log(nLayerDataAccessService.AddPackage("Microsoft.EntityFrameworkCore.SqlServer --version 10.0.4", _appSetting.DataAccessLayerProjectName));
            log(nLayerDataAccessService.AddPackage("Microsoft.EntityFrameworkCore.Tools --version 10.0.4", _appSetting.DataAccessLayerProjectName));

            // 3. Files
            log(nLayerDataAccessService.GenerateStaticFiles("DataAccess", _appSetting.DataAccessLayerProjectName));

            // 4. Repository Services
            log(nLayerDataAccessService.GenerateRepositories());

            // 5. UOW
            log(nLayerDataAccessService.GenerateUOW());

            // 6. Context Fiel
            log(nLayerDataAccessService.GenerateContext());

            // 7. Service Registrations
            log(nLayerDataAccessService.GenerateServiceRegistration());

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

            //// 1. Create Core Class Library if not exists
            //log(nLayerBusinessService.CreateClassLibrearyProject(_appSetting.BusinessLayerProjectName, referances: [$"../{_appSetting.DataAccessLayerProjectName}/{_appSetting.DataAccessLayerProjectName}.csproj"]));

            //// 2. Static Files
            //log(nLayerBusinessService.GenerateStaticFiles("Business", _appSetting.BusinessLayerProjectName, new
            //{
            //    identity_user_type = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository).IdentityUserType
            //}));

            // 3. Mappings
            log(nLayerBusinessService.GenerateMappings());

            //// 5. Concretes
            //log(nLayerBusinessService.GeneraterService());

            //// 6. Service Registrations
            //log(nLayerBusinessService.GenerateServiceRegistrations());

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

            // 1. Create Core Class Library if not exists
            log(nLayerAPIService.CreateProject());

            // 2. Add Packages
            log(nLayerAPIService.AddPackage("Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(nLayerAPIService.AddPackage("Microsoft.AspNetCore.OpenApi --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(nLayerAPIService.AddPackage("Microsoft.EntityFrameworkCore.Design --version 10.0.4", _appSetting.WebAPILayerProjectName));
            log(nLayerAPIService.AddPackage("Scalar.AspNetCore --version 2.14.1", _appSetting.WebAPILayerProjectName));

            // 3. Static Files
            log(nLayerAPIService.GenerateStaticFiles("API", _appSetting.WebAPILayerProjectName, new
            {
                identity_api_registration_code = nLayerAPIService.GetIdentityRegistrationCode(),
                db_connection_name = _appSetting.DBConnectionString,
                securityKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            }));

            // 4. Controllers
            log(nLayerAPIService.GenerateControllers());

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

            // 1. Create Project if not exists
            log(nLayerWebUIService.CreateProject());

            // 2. Add Packages
            log(nLayerWebUIService.AddPackage("FluentValidation.AspNetCore --version 11.3.1", _appSetting.WebUILayerProjectName));
            log(nLayerWebUIService.AddPackage("Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation --version 10.0.4", _appSetting.WebUILayerProjectName));
            log(nLayerWebUIService.AddPackage("Microsoft.EntityFrameworkCore.Design --version 10.0.4", _appSetting.WebUILayerProjectName));

            // 3. Utils
            log(nLayerWebUIService.GenerateStaticFiles("WebUI", _appSetting.WebUILayerProjectName, new
            {
                identity_web_ui_registration_code = nLayerWebUIService.GetIdentityRegistrationCode(),
                db_connection_name = _appSetting.DBConnectionString,
                identity_user_type = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository).IdentityUserType
            }));

            // 4. Side Menu ViewComponent
            log(nLayerWebUIService.GenerateSideMenuViewComponent());

            // 5. ViewModels
            log(nLayerWebUIService.GenerateViewModels());

            // 6. Controllers
            log(nLayerWebUIService.GenerateControllers());

            // 7. Views
            log(nLayerWebUIService.GenerateViews());

            // 8. wwwroot
            log(nLayerWebUIService.Copywwwroot());

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }
}
