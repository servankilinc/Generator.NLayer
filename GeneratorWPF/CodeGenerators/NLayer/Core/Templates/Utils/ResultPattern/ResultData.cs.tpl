using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace {{ core_project_name }}.Utils.ResultPattern;

public class Result<TData> : IResult
{
    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; protected init; }
    public string Message { get; set; } = string.Empty;
    public Error? Error { get; protected init; }
    public TData? Data { get; protected init; }

    public Result(TData data, string? message = default)
    {
        IsSuccess = true;
        Data = data;
        Message = message ?? this.GetDefaultMessage();
    }
    public Result(Error error, string? message = default)
    {
        IsSuccess = false;
        Error = error;
        Message = message ?? this.GetDefaultMessage();
    }


    #region Static Factory Methods
    public static Result<TData> Success(TData data, string? message = default) => new Result<TData>(data, message);

    public static Result<TData> Failure(
        string? message = default,
        string? description = default,
        int code = default,
        Dictionary<string, object?>? metadata = default,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memeberName = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var _metadata = HandleMetadata(metadata, filePath, memeberName, lineNumber);
        return new Result<TData>(Error.Failure(description, code, _metadata), message);
    }

    public static Result<TData> NotFound(
        string? message = default,
        string? description = default,
        int code = default,
        Dictionary<string, object?>? metadata = default,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memeberName = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var _metadata = HandleMetadata(metadata, filePath, memeberName, lineNumber);
        return new Result<TData>(Error.NotFound(description, code, _metadata), message);
    }

    public static Result<TData> Validation(
        Dictionary<string, string[]> validationFailures,
        string? message = default,
        string? description = default,
        int code = default,
        Dictionary<string, object?>? metadata = default,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memeberName = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var _metadata = HandleMetadata(metadata, filePath, memeberName, lineNumber, validationFailures);
        return new Result<TData>(Error.Validation(validationFailures, description, code, _metadata), message);
    }

    public static Result<TData> Forbidden(
        string? message = default,
        string? description = default,
        int code = default,
        Dictionary<string, object?>? metadata = default,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memeberName = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var _metadata = HandleMetadata(metadata, filePath, memeberName, lineNumber);
        return new Result<TData>(Error.Forbidden(description, code, _metadata), message);
    }

    private static Dictionary<string, object?> HandleMetadata(Dictionary<string, object?>? details, string filePath, string memeberName, int lineNumber, Dictionary<string, string[]>? validationFailures = null)
    {
        Dictionary<string, object?> metadata = new()
        {
            { "FilePath", filePath },
            { "MemeberName", memeberName },
            { "LineNumber", lineNumber }
        };
        if (details != default)
        {
            foreach (var param in details)
            {
                metadata[param.Key] = param.Value;
            }
        }
        if (validationFailures != null)
        {
            metadata["ValidationFailures"] = validationFailures;
        }
        return metadata;
    }
    #endregion
}