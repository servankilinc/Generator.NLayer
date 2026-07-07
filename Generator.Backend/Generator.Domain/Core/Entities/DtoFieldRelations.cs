using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class DtoFieldRelations : EntityBase
{
    public int DtoFieldId { get; set; }
    public int RelationId { get; set; }
    public int SequenceNo { get; set; }
    public virtual DtoField DtoField { get; set; } = null!;
    public virtual Relation Relation { get; set; } = null!;
}