namespace Generator.Domain.Core.Dtos.Relation;

public class RelationUpdateDto
{
    public int Id { get; set; }
    public int PrimaryFieldId { get; set; }
    public int ForeignFieldId { get; set; }
    public int RelationTypeId { get; set; }
    public int DeleteBehaviorTypeId { get; set; }
    public string? PrimaryEntityVirPropName { get; set; }
    public string? ForeignEntityVirPropName { get; set; }


    public void Map(ref Entities.Relation relation)
    {
        relation.PrimaryFieldId = PrimaryFieldId;
        relation.ForeignFieldId = ForeignFieldId;
        relation.RelationTypeId = RelationTypeId;
        relation.DeleteBehaviorTypeId = DeleteBehaviorTypeId;
        relation.PrimaryEntityVirPropName = PrimaryEntityVirPropName;
        relation.ForeignEntityVirPropName = ForeignEntityVirPropName;
    }
}
