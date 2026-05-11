using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class FieldType : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SourceTypeId { get; set; }

    public FieldTypeSource? SourceType { get; set; }
    public virtual ICollection<Field>? Fields { get; set; }
}
