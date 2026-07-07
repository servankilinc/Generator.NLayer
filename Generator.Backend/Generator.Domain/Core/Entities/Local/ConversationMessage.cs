using System.ComponentModel.DataAnnotations;

namespace Generator.Domain.Core.Entities.Local;

public class ConversationMessage
{
    [Key]
    public int Id { get; set; }
    public int ConversationId { get; set; }

    /// <summary>"user", "assistant" or "tool" (executed tool-call results).</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>Serialized proposed/executed tool calls belonging to this message, if any.</summary>
    public string? ToolCallsJson { get; set; }
    public DateTime CreateDate { get; set; }

    public Conversation? Conversation { get; set; }
}
