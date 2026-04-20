using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using {{ core_project_name }}.Utils.Logging;
using {{ model_project_name }}.Entities;
using {{ webui_project_name }}.Controllers.Base;
using {{ webui_project_name }}.Models.Auth;

namespace {{ webui_project_name }}.Controllers;

[AllowAnonymous]
public class AccountController : BaseController
{
    private readonly UserManager<{{identity_user_type}}> _userManager;
    private readonly SignInManager<{{identity_user_type}}> _signInManager;
    private readonly ILoggingService _loggingService;
    private readonly IMapper _mapper;
    public AccountController(ILogger<AccountController> logger, UserManager<{{identity_user_type}}> userManager, SignInManager<{{identity_user_type}}> signInManager, IMapper mapper, ILoggingService loggingService) : base(logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _loggingService = loggingService;
        _mapper = mapper;
    }

    [HttpGet]
    public IActionResult Login()
    {
        var model = new LoginRequest();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest loginRequest)
    {
        if (!ModelState.IsValid)
            return View(loginRequest);

        {{identity_user_type}}? user;
        if (!string.IsNullOrEmpty(loginRequest.Email))
        {
            user = await _userManager.FindByEmailAsync(loginRequest.Email);
        }
        else if (!string.IsNullOrEmpty(loginRequest.UserName))
        {
            user = await _userManager.FindByNameAsync(loginRequest.UserName);
        }
        else
        {
            _loggingService.LogWarning($"Email or username is required {loginRequest.Email} {loginRequest.UserName}");
            ModelState.AddModelError("", "Email or username is required");
            return View(loginRequest);
        }

        if (user == null)
        {
            _loggingService.LogWarning($"Email or password is incorrect {loginRequest.Email}");
            ModelState.AddModelError("", "Email or password is incorrect");
            return View(loginRequest);
        }

        var result = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, isPersistent: loginRequest.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _loggingService.LogWarning($"User account is locked, email address: {loginRequest.Email}");
            ModelState.AddModelError("", "Your account is locked.");
            return View(loginRequest);
        }
        else if (!result.Succeeded)
        {
            _loggingService.LogWarning($"Email or password is incorrect {loginRequest.Email}");
            ModelState.AddModelError("", "Email or password is incorrect");
            return View(loginRequest);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignUp(SignUpRequest signUpRequest)
    {
        if (!ModelState.IsValid)
            return View(signUpRequest);

        var userExist = await _userManager.FindByEmailAsync(signUpRequest.Email);
        if (userExist != null)
        {
            _loggingService.LogWarning($"Email is already in use {signUpRequest.Email}");
            ModelState.AddModelError("", "Email is already in use");
            return View(signUpRequest);
        }

        userExist = await _userManager.FindByNameAsync(signUpRequest.UserName);
        if (userExist != null)
        {
            _loggingService.LogWarning($"Username is already in use {signUpRequest.UserName}");
            ModelState.AddModelError("", "Username is already in use");
            return View(signUpRequest);
        }

        var user = _mapper.Map<User>(signUpRequest);
        user.UserName = Guid.NewGuid().ToString();

        var result = await _userManager.CreateAsync(user, signUpRequest.Password);
        if (!result.Succeeded)
        {
            _loggingService.LogWarning($"User cannot be created {signUpRequest.Email} error list: {string.Join("\n", result.Errors.Select(e => e.Description))}");
            ModelState.AddModelError("", "User cannot be created");
            return View(signUpRequest);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");

        var resultSignIn = await _signInManager.PasswordSignInAsync(user, signUpRequest.Password, isPersistent: true, lockoutOnFailure: false);
        if (resultSignIn.IsLockedOut)
        {
            _loggingService.LogWarning($"User account is locked {signUpRequest.Email}");
            ModelState.AddModelError("", "User account is locked");
            return View(signUpRequest);
        }
        else if (!resultSignIn.Succeeded)
        {
            _loggingService.LogWarning($"User created but an error occured on signup process {signUpRequest.Email}");
            ModelState.AddModelError("", "User created successfully.");
            return View(signUpRequest);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> LogOut()
    {
        await _signInManager.SignOutAsync();
        //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}
