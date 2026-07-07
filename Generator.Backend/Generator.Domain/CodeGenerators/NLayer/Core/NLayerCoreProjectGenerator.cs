using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Core.Entities;

namespace Generator.Domain.CodeGenerators.NLayer.Core;

public class NLayerCoreGenerator : IGenerationStep
{
    private readonly DotnetCliService _cli;
    private readonly TemplateRenderer _templateRenderer;

    public string Name => "Core Layer";
    public int Order => 2;
    public int ProgressWeight => 15;

    public NLayerCoreGenerator(DotnetCliService cli, TemplateRenderer templateRenderer)
    {
        _cli = cli;
        _templateRenderer = templateRenderer;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            // 1. Create Core Class Library if not exists
            log(_cli.CreateClassLibraryProject(appSetting, appSetting.CoreLayerProjectName));

            // 3. Add Packages
            log(_cli.AddPackage(appSetting, "AutoMapper --version 14.0.0", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "FluentValidation --version 12.1.1", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "FluentValidation.DependencyInjectionExtensions --version 12.1.1", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "Microsoft.EntityFrameworkCore --version 10.0.4", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "Newtonsoft.Json --version 13.0.4", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "Serilog.AspNetCore --version 10.0.0", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "Serilog.Sinks.Async --version 2.1.0", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "Serilog.Sinks.File --version 7.0.0", appSetting.CoreLayerProjectName));
            log(_cli.AddPackage(appSetting, "System.Linq.Dynamic.Core --version 1.7.1", appSetting.CoreLayerProjectName));

            log(_cli.Restore(appSetting, appSetting.CoreLayerProjectName));

            // 4. Static Files
            log(_templateRenderer.GenerateStaticFiles(appSetting, "Core", appSetting.CoreLayerProjectName));

            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }
}

