using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class Dto : EntityBase
{
    public int Id { get; set; }
    public int RelatedEntityId { get; set; }
    public int CrudTypeId { get; set; }
    public string Name { get; set; } = null!;
    
    public virtual CrudType CrudType { get; set; } = null!;
    public virtual Entity RelatedEntity { get; set; } = null!;
    public virtual ICollection<DtoField> DtoFields { get; set; } = null!;
    public virtual ICollection<Entity> CreateEntities { get; set; } = null!;
    public virtual ICollection<Entity> UpdateEntities { get; set; } = null!;
    public virtual ICollection<Entity> DeleteEntities { get; set; } = null!;
    public virtual ICollection<Entity> ReportEntities { get; set; } = null!;
    public virtual ICollection<Entity> BasicResponseEntities { get; set; } = null!;
    public virtual ICollection<Entity> DetailResponseEntities { get; set; } = null!;


    #region Helpers
    public string ServiceGetListMethodName(Entity entity)
    {
        return this.Id == entity.BasicResponseDtoId ? "GetBaseListAsync" :
                this.Id == entity.DetailResponseDtoId ? "GetDetailListAsync" :
                $"Get{this.Name}ListAsync";
    }
    public string ServiceGetMethodName(Entity entity)
    {
        return this.Id == entity.BasicResponseDtoId ? "GetBaseAsync" :
                this.Id == entity.DetailResponseDtoId ? "GetDetailAsync" :
                $"Get{this.Name}Async";
    }


    public string PresentationLayerListMethodName(Entity entity)
    {
        return this.Id == entity.BasicResponseDtoId ? "GetBaseList" :
                this.Id == entity.DetailResponseDtoId ? "GetDetailList" :
                $"Get{this.Name}List";
    }
    public string PresentationLayerGetMethodName(Entity entity)
    {
        return this.Id == entity.BasicResponseDtoId ? "GetBase" :
                this.Id == entity.DetailResponseDtoId ? "GetDetail" :
                $"Get{this.Name}";
    } 
    #endregion
}