using System.Text;
using Microsoft.AspNetCore.Localization;
using {{ core_project_name }}.Utils.Localization;
using {{ core_project_name }}.Utils;

namespace {{ webui_project_name }}.Utils.Extensions;

public static class HttpContextExtensions
{
    public static bool IsJsonRequest(this HttpContext httpContext)
    {
        return
            httpContext.Request.Headers["X-Request-Provider"].ToString().ToLowerInvariant() == "json" ||
            httpContext.Request.Headers["Accept"].ToString().Contains("application/json") ||
            httpContext.Request.Headers["X-Requested-With"].ToString().Contains("XMLHttpRequest") ||
            httpContext.Request.ContentType?.Contains("application/json") == true;
    }

    public static string GetUrl(this HttpContext httpContext)
    {
        string SchemeDelimiter = Uri.SchemeDelimiter;
        var scheme = httpContext.Request.Scheme ?? string.Empty;
        var host = httpContext.Request.Host.Value ?? string.Empty;
        var pathBase = httpContext.Request.PathBase.Value ?? string.Empty;
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var queryString = httpContext.Request.QueryString.Value ?? string.Empty;

        var length = scheme.Length + SchemeDelimiter.Length + host.Length + pathBase.Length + path.Length + queryString.Length;

        return new StringBuilder(length)
            .Append(scheme)
            .Append(SchemeDelimiter)
            .Append(host)
            .Append(pathBase)
            .Append(path)
            .Append(queryString)
            .ToString();
    }

    public static string GetCurrentLanguage(this HttpContext httpContext)
    {
        var localizationSettings = httpContext.RequestServices.GetRequiredService<LocalizationSettings>();
        string defaultCulture = localizationSettings?.DefaultLanguage.GetDescription() ?? "tr-TR";

        var cookieName = CookieRequestCultureProvider.DefaultCookieName;
        var cookieValue = httpContext.Request.Cookies[cookieName];
        if (string.IsNullOrWhiteSpace(cookieValue)) return defaultCulture;

        var requestCulture = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
        var cultureInfo = requestCulture?.Cultures.FirstOrDefault().Value;
        if (string.IsNullOrWhiteSpace(cultureInfo)) return defaultCulture;

        return cultureInfo;
    }
}