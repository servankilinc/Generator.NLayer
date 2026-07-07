using Generator.Domain.Core.Dtos.DtoField;

namespace Generator.Domain.Core.Dtos.Dto;

public class DtoDetailResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string RelatedEntityName { get; set; } = null!;
    public string CrudTypeName { get; set; } = null!;

    public virtual ICollection<DtoFieldResponseDto> DtoFields { get; set; } = null!;
}
