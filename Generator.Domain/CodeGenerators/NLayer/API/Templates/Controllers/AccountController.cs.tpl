using Microsoft.AspNetCore.Mvc;
using {{ model_project_name }}.Auth.Login;
using {{ model_project_name }}.Auth.Refresh;
using {{ model_project_name }}.Auth.SignUp;
using {{ business_project_name }}.Abstract;
using {{ api_project_name }}.Controllers.Base;

namespace {{ api_project_name }}.Controllers;

[ApiController]
public class AccountController : BaseController
{
    private readonly IAuthService _authService;
    public AccountController(IAuthService authService, ILogger<AccountController> logger) : base(logger) => _authService = authService;


    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return ToAction(result);
    }

    [HttpPost("SignUp")]
    public async Task<IActionResult> SignUp(SignUpRequest request)
    {
        var result = await _authService.SignUpAsync(request);

        return ToAction(result);
    }

    [HttpPost("RefreshAuth")]
    public async Task<IActionResult> RefreshAuth(RefreshAuthRequest request)
    {
        var result = await _authService.RefreshAuthAsync(request);

        return ToAction(result);
    }
}