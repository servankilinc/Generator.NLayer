using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class Relation : EntityBase
{
    public int Id { get; set; }
    public int PrimaryFieldId { get; set; }
    public int ForeignFieldId { get; set; }
    public int RelationTypeId { get; set; }
    public int DeleteBehaviorTypeId { get; set; }
    public string PrimaryEntityVirPropName { get; set; } = null!;
    public string ForeignEntityVirPropName { get; set; } = null!;

    public virtual Field PrimaryField { get; set; } = null!;
    public virtual Field ForeignField { get; set; } = null!;
    public virtual RelationType RelationType { get; set; } = null!;
    public virtual DeleteBehaviorType DeleteBehaviorType { get; set; } = null!;
    public virtual ICollection<DtoFieldRelations>? DtoFieldRelations { get; set; }

    #region Helpers
    public string GetOnDeleteType()
    {
        return this.DeleteBehaviorTypeId switch
        {
            (int)Enums.DeleteBehaviorTypeEnums.Cascade => "DeleteBehavior.Cascade",
            (int)Enums.DeleteBehaviorTypeEnums.ClientCascade => "DeleteBehavior.ClientCascade",
            (int)Enums.DeleteBehaviorTypeEnums.Restrict => "DeleteBehavior.Restrict",
            (int)Enums.DeleteBehaviorTypeEnums.ClientSetNull => "DeleteBehavior.ClientSetNull",
            (int)Enums.DeleteBehaviorTypeEnums.ClientNoAction => "DeleteBehavior.ClientNoAction",
            (int)Enums.DeleteBehaviorTypeEnums.SetNull => "DeleteBehavior.SetNull",
            (int)Enums.DeleteBehaviorTypeEnums.NoAction => "DeleteBehavior.NoAction",
            _ => "DeleteBehavior.Restrict"
        };
    }
    #endregion
}
