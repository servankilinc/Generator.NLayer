using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class ValidatorTypeParam : EntityBase
{
    public int Id { get; set; }
    public int ValidatorTypeId { get; set; }
    public string Key { get; set; } = null!;

    public ValidatorType ValidatorType { get; set; } = null!;
    public virtual ICollection<ValidationParam>? ValidationParams { get; set; }
}
