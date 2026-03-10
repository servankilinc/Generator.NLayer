using System.Runtime.CompilerServices;

namespace {{ core_project_name }}.Utils.Logging;

public interface ILoggingService
{
    void LogInfo(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0);
    void LogWarning(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0);
    void LogError(string message, Exception? exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0);
}
