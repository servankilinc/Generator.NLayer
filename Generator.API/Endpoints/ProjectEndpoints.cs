using Generator.Domain.Core.Entities.Local;
using Generator.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Context;
using Generator.Domain.Services;

namespace Generator.API.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/activeProject", (IProjectProvider projectProvider) =>
        {
            var current = projectProvider.CurrentProject;
            if (current is null)
                return Results.NotFound();
            return Results.Ok(current);
        });

        app.MapPost("/activeProject", (int id, IActiveProjectStore activeProjectStore) =>
        {
            using var localContext = new LocalContext();
            var project = localContext.Projects.FirstOrDefault(x => x.Id == id);
            if (project is null)
                return Results.NotFound();
            activeProjectStore.ActiveProject = project;
            return Results.Ok();
        });

        app.MapGet("/project/list", async () =>
        {
            using var localContext = new LocalContext();
            var result = await localContext.Projects.ToListAsync();
            return Results.Ok(result);
        });

        app.MapPost("/project", async (Project project) =>
        {
            using var localContext = new LocalContext();
            project.CreateDate = DateTime.Now;
            await localContext.Projects.AddAsync(project);
            await localContext.SaveChangesAsync();
            return Results.Ok(project);
        });

        app.MapDelete("/project", async (int id) =>
        {
            using var localContext = new LocalContext();
            var project = localContext.Projects.FirstOrDefault(x => x.Id == id);
            if (project is null)
                return Results.NotFound();

            localContext.Projects.Remove(project);
            await localContext.SaveChangesAsync();
            return Results.Ok();
        });
    }
}
