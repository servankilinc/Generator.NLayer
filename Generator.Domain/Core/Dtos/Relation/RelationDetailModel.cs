namespace Generator.Domain.Core.Dtos.Relation;

public class RelationDetailModel
{
    public int Id { get; set; }
    public int PrimaryEntityId { get; set; }
    public string? PrimaryEntityName { get; set; }
    public int ForeignEntityId { get; set; }
    public string? ForeignEntityName { get; set; }
    public int PrimaryFieldId { get; set; }
    public string? PrimaryFieldName { get; set; }
    public int ForeignFieldId { get; set; }
    public string? ForeignFieldName { get; set; }
    public int RelationTypeId { get; set; }
    public string? RelationTypeName { get; set; }
    public int DeleteBehaviorTypeId { get; set; }
    public string? DeleteBehaviorTypeName { get; set; }
    public string? PrimaryEntityVirPropName { get; set; }
    public string? ForeignEntityVirPropName { get; set; }
}
