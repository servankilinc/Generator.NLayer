using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class DeleteBehaviorType : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual ICollection<Relation>? Relations { get; set; }
}
