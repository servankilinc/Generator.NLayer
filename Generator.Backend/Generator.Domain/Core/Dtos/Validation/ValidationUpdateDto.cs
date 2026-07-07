using Generator.Domain.Core.Dtos.ValidationParam;

namespace Generator.Domain.Core.Dtos.Validation;

public class ValidationUpdateDto
{
    public int ValidationId { get; set; }
    public int DtoFieldId { get; set; }
    // ValidatiorTypeId değişince ValidationParams listesi ve ErrorMessage Değişsin
    // Eğer seçilen ValidatorType'ın ValidatorTypeParams bilgileri varsa liste güncellenir 
    // Seçilen ValidatorType'ın Description bilgisi ErrorMessage a yazılsın
    public int ValidatorTypeId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationParamUpdateDto>? ValidationParams { get; set; }
}