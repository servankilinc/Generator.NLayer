namespace Generator.API.Services.AI;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <param name="ConversationId">Existing conversation to continue. When null a new conversation
/// is created for the active project (falls back to stateless chat if no project is active).</param>
/// <param name="History">Stateless fallback history; ignored when a persisted conversation is used.</param>
public record AiChatRequest(string Prompt, List<ChatMessageDto>? History = null, int? ConversationId = null);

public class ProposedToolCallDto
{
    public string Id { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object?> Arguments { get; set; } = new();
}

public class AiExecuteRequest
{
    public List<ProposedToolCallDto> ToolCalls { get; set; } = new();

    /// <summary>Conversation to record the execution results into, if any.</summary>
    public int? ConversationId { get; set; }
}

public record AiChatResult(string? Response, bool RequiresApproval, List<ProposedToolCallDto>? ProposedChanges, int? ConversationId)
{
    public static AiChatResult FromResponse(string response, int? conversationId) => new(response, false, null, conversationId);
    public static AiChatResult FromProposals(List<ProposedToolCallDto> proposals, int? conversationId) => new(null, true, proposals, conversationId);
}
