using Generator.API.SignalR.Hubs;
using Generator.Domain.CodeGenerators.NLayer;
using Generator.Domain.Core;
using Generator.Domain.Core.Entities.Local;
using Generator.Domain.Services;
using Microsoft.AspNetCore.SignalR;

namespace Generator.API.Endpoints;

public static class GenerationEndpoints
{
    public static void MapGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/start-generate", (IServiceProvider serviceProvider, IHubContext<CommunicationHub> hubContext, IActiveProjectStore activeProjectStore) =>
        {
            Project? project = activeProjectStore.ActiveProject;

            if (project is null)
                return Results.NotFound();

            AppendToResults(hubContext, "Generation Started.");

            Task.Run(() =>
            {
                using var generationScope = serviceProvider.CreateScope();
                var layerGeneratorService = generationScope.ServiceProvider.GetRequiredService<NLayerGeneratorService>();

                SetProgressAmount(hubContext, 0);

                bool result = layerGeneratorService.ExecuteGeneration(
                    msg => AppendToResults(hubContext, msg),
                    pct => SetProgressAmount(hubContext, pct)
                );

                if (!result)
                {
                    AppendToResults(hubContext, "Failed to generate project.");
                    return false;
                }

                SetProgressAmount(hubContext, 100);
                AppendToResults(hubContext, "Project Generated Successfully.");
                return true;
            });

            return Results.Ok();
        });
    }

    private static void AppendToResults(IHubContext<CommunicationHub> hubContext, string message)
    {
        hubContext.Clients.All.SendAsync("AppendToResults", message).GetAwaiter().GetResult();
        Thread.Sleep(500);
    }

    private static void SetProgressAmount(IHubContext<CommunicationHub> hubContext, int rate)
    {
        hubContext.Clients.All.SendAsync("Progress", rate).GetAwaiter().GetResult();
        Thread.Sleep(500);
    }
}
