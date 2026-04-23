using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using FluentValidation;
using {{ core_project_name }};
using {{ core_project_name }}.Utils.Auth;
using {{ model_project_name }}.Entities;
using {{ dataAccess_project_name }}.Contexts;
using {{ dataAccess_project_name }};
using {{ business_project_name }};
using {{ webui_project_name }}.ExceptionHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();


#region ------- CORS -------
builder.Services.AddCors(options =>
{
    options.AddPolicy("policy_cors", builder =>
    {
        builder
            .AllowAnyOrigin()
            //.WithOrigins("https://www.frontend.com")
            //.AllowCredentials() // AllowAnyOrigin and AllowCredentials cannot using together so open when you use WithOrigins
            .AllowAnyMethod()
            .AllowAnyHeader()
            //.WithHeaders("Content-Type", "Authorization")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
#endregion


#region ------- Rate Limiter -------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter(policyName: "policy_rate_limiter", slidingOptions =>
    {
        slidingOptions.PermitLimit = 15;
        slidingOptions.Window = TimeSpan.FromSeconds(5);
        slidingOptions.SegmentsPerWindow = 4;
        slidingOptions.QueueLimit = 5;
        slidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
#endregion


#region ------- Layer Registrations -------
builder.Services.AddCoreServices(builder);
builder.Services.AddDataAccessServices(builder.Configuration);
builder.Services.AddBusinessServices(builder.Configuration);
#endregion


{{ identity_web_ui_registration_code }}


#region ------- Cookie Options -------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(3);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Error/Forbidden";
    options.LogoutPath = "/Account/Logout";
    options.LoginPath = "/Account/Login";
    options.Cookie = new()
    {
        Name = "IdentityCookie",
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        SecurePolicy = CookieSecurePolicy.Always
    };
});
#endregion


#region ------- AutoMapper -------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
#endregion


#region ------- FluentValidation -------
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
#endregion


#region ------- ForwardedHeaders -------
// This middleware extracts the original values of the client request forwarded by the proxy and makes them available to you as first-hand values of the Request object
// so you don't need to directly access HTTP requests to extract the values of X-Forwarded-* headers
// now you can read real client IP with context.Connection.RemoteIpAddress 
//var forwardedHeadersOptions = new ForwardedHeadersOptions
//{
//    ForwardedHeaders =
//        ForwardedHeaders.XForwardedFor |
//        ForwardedHeaders.XForwardedProto,

//    // Add your proxy server IP address here if needed
//    KnownProxies = {
//        IPAddress.Parse("")
//    }
//};
#endregion


var app = builder.Build();

#region ------- Exception Handler -------
app.UseMiddleware<ExceptionHandleMiddleware>();
app.UseStatusCodePagesWithReExecute("/error/{0}");
#endregion

app.UseStaticFiles();


if (app.Environment.IsDevelopment())
{
    #region ------- ForwardedHeaders -------
    //forwardedHeadersOptions.KnownNetworks.Clear();
    //forwardedHeadersOptions.KnownProxies.Clear(); 
    #endregion
}
else
{
    app.UseHsts();

    // ------- ForwardedHeaders -------
    // ###### only use if your app is behind a proxy/reverse proxy ######
    //forwardedHeadersOptions.KnownNetworks.Add(
    //    new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8) // Add your proxy/reverse proxy is in the internal network IP range if needed 
    //);
    // app.UseForwardedHeaders(forwardedHeadersOptions);
    // ------- ForwardedHeaders -------
}


#region ------- Request Logger -------
//app.UseSerilogRequestLogging(options =>
//{
//    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

//    options.GetLevel = (httpContext, elapsed, ex) =>
//    {
//        return (ex != null || httpContext.Response.StatusCode >= 500) ? LogEventLevel.Error :
//        (httpContext.Response.StatusCode >= 400) ? LogEventLevel.Warning : LogEventLevel.Information;
//    };

//    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
//    {
//        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
//        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress != null ? httpContext.Connection.RemoteIpAddress.ToString() : "unknown");
//        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
//    };
//});
#endregion


#region ------- Localization Options -------
var requestLocalizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(requestLocalizationOptions);
#endregion


app.UseHttpsRedirection();

app.UseCors("policy_cors");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets()
    .RequireRateLimiting("policy_rate_limiter");

app.Run();
