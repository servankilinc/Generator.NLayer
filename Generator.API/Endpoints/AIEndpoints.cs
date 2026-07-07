using Generator.API.Services.AI;
using Generator.Domain.Context;
using Generator.Domain.Core.Entities.Local;
using Microsoft.EntityFrameworkCore;

namespace Generator.API.Endpoints;

public static class AIEndpoints
{
    public static void MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        MapChatEndpoints(app);
        MapConversationEndpoints(app);
    }

    private static void MapChatEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/generation/ai-setup").WithTags("AI Assistant");

        group.MapPost("/", async (AiChatRequest request, AiChatService chatService, CancellationToken cancellationToken) =>
        {
            var result = await chatService.ChatAsync(request, cancellationToken);

            if (result.RequiresApproval)
            {
                return Results.Ok(new
                {
                    requiresApproval = true,
                    message = "Aşağıdaki değişiklikleri yapmayı planlıyorum. Lütfen kontrol edip onaylayın.",
                    proposedChanges = result.ProposedChanges,
                    conversationId = result.ConversationId
                });
            }

            return Results.Ok(new { response = result.Response, conversationId = result.ConversationId });
        });

        group.MapPost("/execute", async (AiExecuteRequest request, AiChatService chatService, CancellationToken cancellationToken) =>
        {
            var results = await chatService.ExecuteToolCallsAsync(request, cancellationToken);
            return Results.Ok(new { results });
        });
    }

    private static void MapConversationEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/conversation/list", async (int projectId) =>
        {
            using var localContext = new LocalContext();
            var conversations = await localContext.Conversations
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.LastMessageDate ?? c.CreateDate)
                .ToListAsync();
            return Results.Ok(conversations);
        });

        app.MapGet("/conversation/{id:int}", async (int id) =>
        {
            using var localContext = new LocalContext();
            var conversation = await localContext.Conversations
                .Include(c => c.Messages.OrderBy(m => m.Id))
                .FirstOrDefaultAsync(c => c.Id == id);
            if (conversation is null)
                return Results.NotFound();
            return Results.Ok(conversation);
        });

        app.MapGet("/conversation/{id:int}/messages", async (int id) =>
        {
            using var localContext = new LocalContext();
            var exists = await localContext.Conversations.AnyAsync(c => c.Id == id);
            if (!exists)
                return Results.NotFound();

            var messages = await localContext.ConversationMessages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.Id)
                .ToListAsync();
            return Results.Ok(messages);
        });

        app.MapPost("/conversation", async (Conversation conversation) =>
        {
            using var localContext = new LocalContext();
            var projectExists = await localContext.Projects.AnyAsync(p => p.Id == conversation.ProjectId);
            if (!projectExists)
                return Results.NotFound($"Project '{conversation.ProjectId}' not found.");

            conversation.CreateDate = DateTime.Now;
            await localContext.Conversations.AddAsync(conversation);
            await localContext.SaveChangesAsync();
            return Results.Ok(conversation);
        });

        app.MapPut("/conversation/{id:int}", async (int id, Conversation updated) =>
        {
            using var localContext = new LocalContext();
            var conversation = await localContext.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (conversation is null)
                return Results.NotFound();

            conversation.Title = updated.Title;
            await localContext.SaveChangesAsync();
            return Results.Ok(conversation);
        });

        app.MapDelete("/conversation/{id:int}", async (int id) =>
        {
            using var localContext = new LocalContext();
            var conversation = await localContext.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (conversation is null)
                return Results.NotFound();

            localContext.Conversations.Remove(conversation);
            await localContext.SaveChangesAsync();
            return Results.Ok();
        });
    }
}
