using {{ core_project_name }}.Utils.CriticalData;
using FluentValidation;

namespace {{ webui_project_name }}.Models.Auth;

public class LoginRequest
{
    public string? Email { get; set; }
    public string? UserName { get; set; }

    [CriticalData]
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; } = false;
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(b => b).Must(b => !string.IsNullOrWhiteSpace(b.Email) || !string.IsNullOrWhiteSpace(b.UserName)).WithMessage("Either Email or UserName must be provided.");
        RuleFor(b => b.UserName).MinimumLength(6).When(b => !string.IsNullOrWhiteSpace(b.UserName));
        RuleFor(b => b.Email).EmailAddress().When(b => !string.IsNullOrWhiteSpace(b.Email));
        RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);
    }
}