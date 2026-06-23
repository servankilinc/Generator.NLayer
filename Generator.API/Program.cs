using Generator.API.SignalR.Hubs;
using Generator.API.Services;
using Generator.Domain.CodeGenerators.NLayer;
using Generator.Domain.CodeGenerators.NLayer.Core;
using Generator.Domain.CodeGenerators.NLayer.Model;
using Generator.Domain.CodeGenerators.NLayer.DataAccess;
using Generator.Domain.CodeGenerators.NLayer.Business;
using Generator.Domain.CodeGenerators.NLayer.API;
using Generator.Domain.CodeGenerators.NLayer.WebUI;
using Generator.Domain.Repository;
using Generator.Domain.Context;
using Generator.Domain.Services;
using Generator.Domain.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Generator.API.Endpoints;
using Generator.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("allow-policy",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IActiveProjectStore, ActiveProjectStore>();
builder.Services.AddScoped<IProjectProvider, ProjectProvider>();

builder.Services.AddDbContext<ProjectContext>((serviceProvider, optionsBuilder) =>
{
    var projectProvider = serviceProvider.GetRequiredService<IProjectProvider>();
    var currentProject = projectProvider.CurrentProject;
    if (currentProject == null)
    {
        optionsBuilder.UseSqlite("Data Source=fallback.db");
    }
    else
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, $"{currentProject.ProjectName.Replace(' ', '_')}Database.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
});

builder.Services.AddScoped<AppSettingsRepository>();
builder.Services.AddScoped<CrudTypeRepository>();
builder.Services.AddScoped<DeleteBehaviorTypeRepository>();
builder.Services.AddScoped<DtoFieldRelationsRepository>();
builder.Services.AddScoped<DtoFieldRepository>();
builder.Services.AddScoped<DtoRepository>();
builder.Services.AddScoped<EntityRepository>();
builder.Services.AddScoped<FieldRepository>();
builder.Services.AddScoped<FieldTypeRepository>();
builder.Services.AddScoped<RelationRepository>();
builder.Services.AddScoped<ValidatorTypeRepository>();
builder.Services.AddScoped<ValidatorTypeParamRepository>();
builder.Services.AddScoped<ValidationRepository>();

builder.Services.AddScoped<AppSetting>(serviceProvider =>
{
    var repository = serviceProvider.GetRequiredService<AppSettingsRepository>();
    var appSetting = repository.Get(f => f.Id == 1);
    if (appSetting == null)
    {
        throw new InvalidOperationException("App Settings Not Completed To Generate!");
    }
    return appSetting;
});

builder.Services.AddScoped<Generator.Domain.CodeGenerators.Services.DotnetCliService>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Services.FileSystemService>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Services.TemplateRenderer>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Services.RoslynSyntaxHelper>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Services.ValidationRuleGenerator>();

builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerCoreGenerator>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerModelGenerator>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerDataAccessGenerator>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerBusinessGenerator>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerAPIService>();
builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.IGenerationStep, NLayerWebUIGenerator>();

builder.Services.AddScoped<Generator.Domain.CodeGenerators.Pipeline.GenerationPipeline>();

builder.Services.AddScoped<NLayerGeneratorService>();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors("allow-policy");
app.UseExceptionHandling();

app.MapHub<CommunicationHub>("/comunication-hub");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProjectEndpoints();
app.MapEntityEndpoints();
app.MapFieldEndpoints();
app.MapRelationEndpoints();
app.MapDtoEndpoints();
app.MapValidationEndpoints();
app.MapAppSettingEndpoints();
app.MapGenerationEndpoints();

app.Run();