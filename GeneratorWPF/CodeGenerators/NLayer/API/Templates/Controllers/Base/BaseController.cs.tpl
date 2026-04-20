using Microsoft.AspNetCore.Mvc;
using {{ core_project_name }}.Utils.ResultPattern;
using {{ core_project_name }}.Utils;
using {{ core_project_name }}.Enums;

namespace {{ api_project_name }}.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    private readonly ILogger _logger;
    public BaseController(ILogger logger) => _logger = logger;


    protected IActionResult ToAction(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        LogFailedProcess(result);

        var problemDetails = result.GetProblemDetail();
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    protected IActionResult ToAction<TData>(Result<TData> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);
        
        LogFailedProcess(result);

        var problemDetails = result.GetProblemDetail();
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    private void LogFailedProcess(Core.Utils.ResultPattern.IResult result)
    {
        if (result.Error == null) return;

        switch (result.Error.Type)
        {
            case ErrorType.Failure:
                _logger.LogError("Failure process: \nMessage: {Message} \nDetail: {@Error}",
                    result.Message,
                    result.Error
                );
                break;
            case ErrorType.NotFound:
                _logger.LogWarning("Not found: \nMessage: {Message} \nDetail: {@Error}",
                    result.Message,
                    result.Error
                );
                break;
            case ErrorType.Validation:
                _logger.LogWarning("Validation error: \nMessage: {Message} \nDetail: {@Error}",
                    result.Message,
                    result.Error
                );
                break;
            case ErrorType.Forbidden:
                _logger.LogWarning("Forbidden : \nMessage: {Message} \nDetail: {@Error}",
                    result.Message,
                    result.Error
                );
                break;
            default:
                _logger.LogError("Unknown error: \nMessage: {Message} \nDetail: {@Error}",
                    result.Message,
                    result.Error
                );
                break;
        }
    }
}
