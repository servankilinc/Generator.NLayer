using Generator.Domain.Core.Entities;

namespace Generator.Domain.CodeGenerators.Pipeline;

/// <summary>
/// Orchestrates a sequence of <see cref="IGenerationStep"/> instances,
/// executing them in order and reporting progress via callbacks.
/// </summary>
public class GenerationPipeline
{
    private readonly IEnumerable<IGenerationStep> _steps;

    public GenerationPipeline(IEnumerable<IGenerationStep> steps)
    {
        _steps = steps;
    }

    /// <summary>
    /// Runs all generation steps in order.
    /// </summary>
    /// <param name="appSetting">Shared generation context.</param>
    /// <param name="log">Callback for log messages.</param>
    /// <param name="progress">Callback for progress percentage (0-100).</param>
    /// <returns>True if all steps succeeded, false if any step failed.</returns>
    public bool Run(AppSetting appSetting, Action<string> log, Action<int> progress)
    {
        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();
        int totalWeight = orderedSteps.Sum(s => s.ProgressWeight);
        int completedWeight = 0;

        foreach (var step in orderedSteps)
        {
            log($"▶ Starting: {step.Name}");

            var success = step.Execute(appSetting, log);
            if (!success)
            {
                log($"✖ Failed: {step.Name}");
                return false;
            }

            completedWeight += step.ProgressWeight;
            int percentage = (int)(completedWeight * 100.0 / totalWeight);
            progress(percentage);
            log($"✔ Completed: {step.Name}");
        }

        return true;
    }
}
