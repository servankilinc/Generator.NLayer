using System.ComponentModel;
using Generator.Domain.Repository;
using Microsoft.SemanticKernel;
using Generator.Domain.Core.Dtos.AppSetting;
using Generator.Domain.Context;

namespace Generator.API.Services.AI.Plugins;

public class SettingsPlugin
{
    private readonly AppSettingsRepository _appSettingsRepo;
    private readonly ProjectContext _context;

    public SettingsPlugin(AppSettingsRepository appSettingsRepo, ProjectContext context)
    {
        _appSettingsRepo = appSettingsRepo;
        _context = context;
    }

    [KernelFunction("UpdateSettings")]
    [Description("Updates the global project settings such as Project Name, Solution Name, and DB Connection String.")]
    public string UpdateSettings(
        [Description("The new Project Name")] string projectName,
        [Description("The new Solution Name")] string solutionName,
        [Description("The new Database Connection String")] string dbConnectionString)
    {
        try
        {
            var currentSettings = _appSettingsRepo.Get(f => f.Id == 1);
            if (currentSettings == null) return "Error: Settings table data not found.";

            currentSettings.ProjectName = projectName;
            currentSettings.SolutionName = solutionName;
            currentSettings.DBConnectionString = dbConnectionString;

            _appSettingsRepo.Update(currentSettings);
            return "Project settings successfully updated.";
        }
        catch (Exception ex)
        {
            return $"Error updating settings: {ex.Message}";
        }
    }

    [InspectorFunction]
    [KernelFunction("GetSettings")]
    [Description("Retrieves the current global settings of the .NET project (e.g., Project Name, Solution Name). Use this to check the project's state.")]
    public string GetSettings()
    {
        var settings = _appSettingsRepo.Get(f => f.Id == 1);
        if (settings == null) return "No settings found.";

        return $"Current Project Name: {settings.ProjectName}, Solution Name: {settings.SolutionName}, DB Connection: {settings.DBConnectionString}";
    }

    [InspectorFunction]
    [KernelFunction("GetSystemDictionaryTypes")]
    [Description("Retrieves the system's dynamic lookup tables such as Relation Types, Validator Types, CRUD Types, and Delete Behavior Types. Use this to find the correct ID values before making a relation or validation.")]
    public string GetSystemDictionaryTypes()
    {
        try
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== Relation Types ===");
            foreach(var rt in _context.RelationTypes.ToList())
                sb.AppendLine($"- ID: {rt.Id} | Name: {rt.Name}");

            sb.AppendLine("\n=== Delete Behavior Types ===");
            foreach (var dt in _context.DeleteBehaviorTypes.ToList())
                sb.AppendLine($"- ID: {dt.Id} | Name: {dt.Name}");

            sb.AppendLine("\n=== Validator Types ===");
            foreach (var vt in _context.ValidatorTypes.ToList())
                sb.AppendLine($"- ID: {vt.Id} | Name: {vt.Name}");

            sb.AppendLine("\n=== Field Types ===");
            foreach (var ft in _context.FieldTypes.Where(f => f.SourceTypeId == 1).ToList()) // SourceType=1 Base Types
                sb.AppendLine($"- ID: {ft.Id} | Name: {ft.Name}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading dictionary types: {ex.Message}";
        }
    }
}
