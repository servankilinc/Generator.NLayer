using Generator.Domain.Core.Dtos.DtoField;

namespace Generator.Domain.Core.Dtos.Dto;

public class DtoCreateDto
{
    public string Name { get; set; } = null!;
    public int RelatedEntityId { get; set; }
    public int CrudTypeId { get; set; }
    public List<DtoFieldCreateDto>? DtoFields { get; set; }
}
