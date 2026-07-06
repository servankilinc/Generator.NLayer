using Generator.Domain.Core.Entities;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Generator.Domain.CodeGenerators.Services;

/// <summary>
/// Handles all dotnet CLI operations: creating solutions, class libraries, adding packages, restoring.
/// Extracted from NLayerGeneratorBase.
/// </summary>
public class DotnetCliService
{
    public string CreateSolution(AppSetting appSetting)
    {
        try
        {
            string slnPath = Path.Combine(appSetting.SolutionPath, $"{appSetting.SolutionName}.sln");
            string slnxPath = Path.Combine(appSetting.SolutionPath, $"{appSetting.SolutionName}.slnx");

            if (!Directory.Exists(appSetting.SolutionPath))
                Directory.CreateDirectory(appSetting.SolutionPath);

            if (File.Exists(slnPath) || File.Exists(slnxPath))
                return "INFO: Solution already exists.";

            RunCommand(appSetting.SolutionPath, "dotnet", $"new sln  -n {appSetting.SolutionName}");

            return "OK: Solution Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the Solution. \n\t Details:{ex.Message}");
        }
    }

    public string CreateClassLibraryProject(AppSetting appSetting, string layerProjectName, string[]? references = null)
    {
        try
        {
            string layerPath = Path.Combine(appSetting.SolutionPath, layerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{layerProjectName}.csproj");

            if (Directory.Exists(layerPath) && File.Exists(csprojPath))
                return $"INFO: {layerProjectName} already exists.";

            bool isSlnx = File.Exists(Path.Combine(appSetting.SolutionPath, $"{appSetting.SolutionName}.slnx"));

            RunCommand(appSetting.SolutionPath, "dotnet", $"new classlib -n {layerProjectName}");
            RunCommand(appSetting.SolutionPath, "dotnet", $"sln {appSetting.SolutionName}.{(isSlnx ? "slnx" : "sln")} add {layerProjectName}/{layerProjectName}.csproj");

            var fs = new FileSystemService();
            fs.RemoveFile(layerPath, "Class1.cs");

            if (references != null && references.Length > 0)
            {
                foreach (var reference in references)
                {
                    RunCommand(layerPath, "dotnet", $"add reference {reference}");
                }
            }

            return $"OK: {layerProjectName} Layer Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the {layerProjectName}. \n\t Details:{ex.Message}");
        }
    }

    public string AddPackage(AppSetting appSetting, string packageName, string layerProjectName)
    {
        try
        {
            string layerPath = Path.Combine(appSetting.SolutionPath, layerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{layerProjectName}.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"{layerProjectName}.csproj not found for adding package({layerProjectName}).");

            var doc = XDocument.Load(csprojPath);
            string searchParam = Regex.Replace(packageName, @"\s*--version.*", "").Trim();
            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == searchParam);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in {layerProjectName}.";

            RunCommand(layerPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to {layerProjectName}.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding package to {layerProjectName}. \n\t Details:{ex.Message}");
        }
    }

    public string Restore(AppSetting appSetting, string layerProjectName)
    {
        try
        {
            string layerPath = Path.Combine(appSetting.SolutionPath, layerProjectName);

            RunCommand(layerPath, "dotnet", "restore");
            return $"OK: Restored {layerProjectName}.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring {layerProjectName}. \n Details:{ex.Message}");
        }
    }

    public string RunCommand(string workingDirectory, string fileName, string arguments)
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
}
