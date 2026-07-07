using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class RelationType : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public virtual ICollection<Relation>? Relations { get; set; }
}
