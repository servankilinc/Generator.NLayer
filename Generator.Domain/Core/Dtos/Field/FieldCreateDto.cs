namespace Generator.Domain.Core.Dtos.Field;

public class FieldCreateDto
{
    public int EntityId { get; set; }
    public int FieldTypeId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public bool IsList { get; set; }
    public bool Filterable { get; set; }
}
