namespace Generator.Domain.CodeGenerators.Services;

/// <summary>
/// Handles file and directory operations for code generation output.
/// Extracted from NLayerGeneratorBase.
/// </summary>
public class FileSystemService
{
    public string AddFile(string folderPath, string fileName, string code)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File {fileName} added.";
            }
            else
            {
                return $"INFO: File {fileName} already exists.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}). \n Details:{ex.Message}");
        }
    }

    public string RemoveFile(string folderPath, string fileName)
    {
        try
        {
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return $"OK: File {fileName} removed.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) \n Details: {ex.Message}");
        }
    }

    public string RemoveFolder(string folderPath)
    {
        try
        {
            string filePath = Path.Combine(folderPath);

            if (Directory.Exists(filePath))
            {
                Directory.Delete(filePath, true);
                return $"OK: Folder {folderPath} removed.";
            }
            else
            {
                return $"INFO: Folder {folderPath} does not exist.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing folder ({folderPath}) \n Details: {ex.Message}");
        }
    }

    public string CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string targetFilePath = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, targetFilePath, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string targetSubDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, targetSubDir);
        }

        return $"OK: Directory {sourceDir} copied successfully.";
    }
}
