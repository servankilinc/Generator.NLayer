using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.Core.Entities;

namespace Generator.Domain.CodeGenerators.NLayer;

public class NLayerGeneratorService
{
    private readonly GenerationPipeline _pipeline;
    private readonly AppSetting _appSetting;

    public NLayerGeneratorService(
        GenerationPipeline pipeline,
        AppSetting appSetting)
    {
        _pipeline = pipeline;
        _appSetting = appSetting;
    }

    public bool ExecuteGeneration(Action<string> log, Action<int> progress)
    {
        try
        {
            if (_appSetting == null
            || string.IsNullOrEmpty(_appSetting.Path)
            || string.IsNullOrEmpty(_appSetting.SolutionName)
            || string.IsNullOrEmpty(_appSetting.ProjectName))
        {
            throw new InvalidOperationException("App Settings Not Completed To Generate!");
        }

        return _pipeline.Run(_appSetting, log, progress);
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }
}
