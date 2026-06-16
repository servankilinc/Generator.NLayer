using {{ core_project_name }}.Enums;
using {{ webui_project_name }}.Utils.Extensions;

namespace {{ webui_project_name }}.ExceptionHandler;

public class ExceptionHandleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandleMiddleware> _logger;
    public ExceptionHandleMiddleware(RequestDelegate next, ILogger<ExceptionHandleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            if (context.IsJsonRequest())
            {
                await CatchJsonExceptionAsync(context, e);
            }
            else
            {
                CatchPageException(context, e);
            }
        }
    }


    private void CatchPageException(HttpContext httpContext, Exception exception)
    {
        //response.Clear(); 

        var traceId = httpContext.TraceIdentifier;
        _logger.LogError(exception, "An error occurred during the process. TraceId: {TraceId}, Message: {Message}, InnerException: {InnerException}", traceId, exception.Message, exception.InnerException?.Message ?? string.Empty);
        httpContext.Response.Redirect("/error/500");
    }

    private async Task CatchJsonExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var traceId = httpContext.TraceIdentifier;
        _logger.LogError(exception, "An error occurred during the process. TraceId: {TraceId}, Message: {Message}, InnerException: {InnerException}", traceId, exception.Message, exception.InnerException?.Message ?? string.Empty);

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails()
        {
            Type = $"problems/{ErrorType.Failure}",
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Extensions =
            {
                ["Code"] = (byte)ErrorType.Failure,
                ["traceId"] = traceId
            }
        });
    }
}