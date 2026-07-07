using Generator.Domain.Core.Entities;
using Scriban;

namespace Generator.Domain.CodeGenerators.Services;

/// <summary>
/// Handles Scriban template parsing, caching, and rendering.
/// Also manages static file generation from template directories.
/// Extracted from NLayerGeneratorBase.
/// </summary>
public class TemplateRenderer
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Template> _templateCache = new();

    private readonly FileSystemService _fileSystemService;

    public TemplateRenderer(FileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public string Render(string templateText, object model)
    {
        var template = _templateCache.GetOrAdd(templateText, text =>
        {
            var parsed = Template.Parse(text);
            if (parsed.HasErrors)
                throw new InvalidOperationException(string.Join("\n", parsed.Messages));
            return parsed;
        });

        return template.Render(model, member => member.Name);
    }

    public string ReadTemplate(string rootPath, string filePath)
    {
        return File.ReadAllText(Path.Combine(rootPath, filePath));
    }

    public IEnumerable<string> GetAllTemplateResources(string rootPath)
    {
        return Directory.GetFiles(rootPath, "*.tpl", SearchOption.AllDirectories);
    }

    public string GetTemplatesRootPath(string layer)
    {
        return Path.Combine(AppContext.BaseDirectory, "CodeGenerators", "NLayer", layer, "Templates");
    }

    /// <summary>
    /// Generates all static files from a layer's Templates directory.
    /// Processes .tpl files with Scriban and writes the output to the target layer folder.
    /// </summary>
    public string GenerateStaticFiles(AppSetting appSetting, string layerName, string layerProjectName, object? extendedOptions = null)
    {
        List<string> resultLogs = new List<string>();

        string layerPath = Path.Combine(appSetting.SolutionPath, layerProjectName);
        string rootPath = GetTemplatesRootPath(layerName);

        var resources = GetAllTemplateResources(rootPath);
        foreach (var resourcePath in resources)
        {
            string relativePath = resourcePath.Replace(rootPath, "").Replace(".tpl", "");

            string fileDirectory = Path.GetDirectoryName(relativePath)!;
            fileDirectory = fileDirectory.TrimStart('\\', '/');
            string fileName = Path.GetFileName(relativePath);
            string folderPath = Path.Combine(layerPath, fileDirectory);

            if (File.Exists(Path.Combine(folderPath, fileName)))
            {
                resultLogs.Add($"INFO: File already exists: {fileName}");
                continue;
            }

            string template = File.ReadAllText(resourcePath);

            IDictionary<string, object> dict = new Dictionary<string, object>
            {
                ["core_project_name"] = appSetting.CoreLayerProjectName,
                ["model_project_name"] = appSetting.ModelLayerProjectName,
                ["dataAccess_project_name"] = appSetting.DataAccessLayerProjectName,
                ["business_project_name"] = appSetting.BusinessLayerProjectName,
                ["api_project_name"] = appSetting.WebAPILayerProjectName,
                ["webui_project_name"] = appSetting.WebUILayerProjectName,
                ["project_name"] = appSetting.ProjectName!
            };

            if (extendedOptions != null)
            {
                foreach (var prop in extendedOptions.GetType().GetProperties())
                {
                    var value = prop.GetValue(extendedOptions);
                    if (value != null)
                        dict[prop.Name] = value;
                }
            }

            string content = Render(template, dict);

            resultLogs.Add(_fileSystemService.AddFile(folderPath, fileName, content));
        }

        return string.Join("\n", resultLogs);
    }
}
