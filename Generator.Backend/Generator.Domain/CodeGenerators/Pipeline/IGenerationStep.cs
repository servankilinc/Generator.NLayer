namespace Generator.Domain.CodeGenerators.Pipeline;

using System;
using Generator.Domain.Core.Entities;

/// <summary>
/// Represents a single step in the code generation pipeline.
/// Each layer (Core, Model, DataAccess, Business, API, WebUI) implements this interface.
/// </summary>
public interface IGenerationStep
{
    /// <summary>Display name of the generation step (e.g., "Core Layer").</summary>
    string Name { get; }

    /// <summary>Execution order within the pipeline. Lower values execute first.</summary>
    int Order { get; }

    /// <summary>
    /// Relative weight of this step in the overall progress calculation.
    /// The pipeline normalizes all weights to calculate percentage progress.
    /// </summary>
    int ProgressWeight { get; }

    /// <summary>
    /// Executes the generation step.
    /// </summary>
    /// <param name="context">Shared generation context containing configuration and cached data.</param>
    /// <param name="log">Callback to report log messages to the client.</param>
    /// <returns>True if the step succeeded, false if it failed.</returns>
    bool Execute(AppSetting appSetting, Action<string> log);
}
