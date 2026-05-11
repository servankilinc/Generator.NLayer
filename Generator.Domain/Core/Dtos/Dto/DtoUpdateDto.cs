namespace Generator.Domain.Core.Dtos.Dto;

public class DtoUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int RelatedEntityId { get; set; }
    public int CrudTypeId { get; set; }
}
