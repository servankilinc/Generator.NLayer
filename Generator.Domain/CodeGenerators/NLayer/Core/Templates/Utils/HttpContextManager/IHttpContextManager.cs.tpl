using {{ core_project_name }}.Enums;
using {{ core_project_name }}.Utils.ResultPattern;

namespace {{ core_project_name }}.Utils.HttpContextManager
{
    public interface IHttpContextManager
    {
        Result<string> GetNameIdentifier();
        Result<string> GetUserAgent();
        Result<string> GetClientIp();
        Result<string> GetCurrentCulture();
        Result<byte> GetCurrentLanguageId();
        Result<Language> GetCurrentLanguage();
        Result SetCurrentCulture(string culture);
        Result<string> GetRefreshTokenFromCookie();
        Result AddRefreshTokenToCookie(string refreshToken, DateTime expirationUtc);
        Result DeletetRefreshTokenFromCookie();
    }
}