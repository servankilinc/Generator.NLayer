using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using FluentValidation;
using {{ core_project_name }};
using {{ core_project_name }}.Utils.Auth;
using {{ model_project_name }}.Entities;
using {{ dataAccess_project_name }};
using {{ dataAccess_project_name }}.Contexts;
using {{ business_project_name }};
using {{ api_project_name }}.ExceptionHandler;
using {{ api_project_name }}.Utils;

var builder = WebApplication.CreateBuilder(args);


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

{{ identity_api_registration_code }}

#region ------- AutoMapper -------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
#endregion


#region ------- FluentValidation -------
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
#endregion


builder.Services.AddExceptionHandler<ExceptionHandleMiddleware>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ScalarSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseExceptionHandler();

//app.UseStaticFiles();

//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();
app.MapScalarApiReference();
//}

app.UseHttpsRedirection();

app.UseCors("policy_cors");

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("policy_rate_limiter");

app.MapHealthChecks("/health").RequireHost("localhost");

app.Run();
