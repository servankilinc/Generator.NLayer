using Microsoft.AspNetCore.Mvc;
using {{ core_project_name }}.Utils.ResultPattern;
using {{ core_project_name }}.Utils.Validation;
using {{ core_project_name }}.Enums;
using {{ core_project_name }}.Utils;

namespace {{ webui_project_name }}.Controllers.Base;

public abstract class BaseController : Controller
{
    private readonly ILogger _logger;
    public BaseController(ILogger logger) => _logger = logger;


    /// <summary>
    /// Handle result and return Json content
    /// </summary>
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


    /// <summary>
    /// Handle result and return error view
    /// </summary>
    protected IActionResult ToErrorView(Result result, string? returnUrl = default)
    {
        if (result.IsSuccess)
            return RedirectToAction("Index", "Home");

        LogFailedProcess(result);

        if (returnUrl != null)
            return Redirect(returnUrl);

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(),
            ErrorType.Forbidden => Forbid(),
            ErrorType.Validation => BadRequest(result.Message),
            ErrorType.Failure => BadRequest(result.Message),
            _ => StatusCode(500)
        };
    }


    protected void AddValidationFailuresToModel(ValidatorResult validatorResult)
    {
        if (validatorResult.Failures != null)
        {
            foreach (var failure in validatorResult.Failures)
                foreach (var errorMessage in failure.Value)
                    ModelState.AddModelError(failure.Key, errorMessage);
        }
    }


    private void LogFailedProcess({{ core_project_name }}.Utils.ResultPattern.IResult result)
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
