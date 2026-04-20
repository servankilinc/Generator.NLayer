using AutoMapper;
using FluentValidation;
using {{ model_project_name }}.Entities;

namespace {{ webui_project_name }}.Models.Auth;

public class SignUpRequest
{
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool RememberMe { get; set; } = false;
}

public class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleFor(b => b.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(b => b.UserName).NotNull().NotEmpty().MinimumLength(6);
        RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);
    }
}

public class SignUpRequestMappingProfile : Profile
{
    public SignUpRequestMappingProfile()
    {
        CreateMap<SignUpRequest, User>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) => !Equals(srcMember, destMember)));
    }
}