using Generator.Domain.Core.Entities.Base;

namespace Generator.Domain.Core.Entities;

public class ValidationParam : EntityBase
{
    public int ValidationId { get; set; }
    public int ValidatorTypeParamId { get; set; }
    public string Value { get; set; } = null!;

    public Validation Validation { get; set; } = null!;
    public ValidatorTypeParam ValidatorTypeParam { get; set; } = null!;
}