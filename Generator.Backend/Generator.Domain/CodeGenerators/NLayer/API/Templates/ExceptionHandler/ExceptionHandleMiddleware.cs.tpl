using Microsoft.AspNetCore.Diagnostics;

namespace {{ api_project_name }}.ExceptionHandler;

public class ExceptionHandleMiddleware : IExceptionHandler
{
    private readonly ILogger<ExceptionHandleMiddleware> _logger;
    public ExceptionHandleMiddleware(ILogger<ExceptionHandleMiddleware> logger) => _logger = logger;


    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        _logger.LogError(exception, "An error occurred during the process. TraceId: {TraceId}, Message: {Message}, InnerException: {InnerException}", traceId, exception.Message, exception.InnerException?.Message ?? string.Empty);

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = $"http://{{ project_name }}.com/problems/InternalServerError",
            Title = "An error occurred",
            Extensions =
            {
                ["traceId"] = traceId
            }
        });
        return true;
    }
}