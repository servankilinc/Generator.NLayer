using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class Service : EntityBase
{
    public int Id { get; set; }
    public int ServiceLayerId { get; set; }
    public int RelatedEntityId { get; set; }

    public ServiceLayer ServiceLayer { get; set; } = null!;
    public Entity RelatedEntity { get; set; } = null!;
}
