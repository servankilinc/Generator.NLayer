using System.Security.Claims;
using {{ core_project_name }}.Utils.Auth;
using {{ core_project_name }}.Utils.ResultPattern;
using {{ model_project_name }}.Entities;

namespace {{ business_project_name }}.Utils.TokenService;

public interface ITokenService
{
    Result<AccessToken> GenerateAccessToken(IList<Claim> claims);
    Result<RefreshToken> GenerateRefreshToken({{ identity_user_type }} user, string tokenValue, string clientType, Guid? deviceId = default);
    string GenerateRandomNumber();
    string HashToken(string token);
}