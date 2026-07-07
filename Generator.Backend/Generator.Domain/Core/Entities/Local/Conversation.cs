using System.ComponentModel.DataAnnotations;

namespace Generator.Domain.Core.Entities.Local;

public class Conversation
{
    [Key]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime? LastMessageDate { get; set; }

    public Project? Project { get; set; }
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
