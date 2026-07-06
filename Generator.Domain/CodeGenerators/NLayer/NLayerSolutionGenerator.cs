using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Core.Entities;

namespace Generator.Domain.CodeGenerators.NLayer;

public class NLayerSolutionGenerator : IGenerationStep
{
    private readonly DotnetCliService _cli;

    public string Name => "Solution Initialization";
    public int Order => 1;
    public int ProgressWeight => 5;

    public NLayerSolutionGenerator(DotnetCliService cli)
    {
        _cli = cli;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            log(_cli.CreateSolution(appSetting));
            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }
}
