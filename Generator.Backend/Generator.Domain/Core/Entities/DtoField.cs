using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class DtoField : EntityBase
{
    public int Id { get; set; }
    public int DtoId { get; set; }
    public int SourceFieldId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }

    public virtual Dto Dto { get; set; } = null!;
    public virtual Field SourceField { get; set; } = null!;
    public virtual ICollection<Validation>? Validations { get; set; }
    public virtual ICollection<DtoFieldRelations>? DtoFieldRelations { get; set; }
}