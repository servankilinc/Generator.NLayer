using Generator.Domain.Core.Dtos.Field;

namespace Generator.Domain.Core.Dtos.Entity;

public class EntityCreateDto
{
    public string Name { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public bool SoftDeletable { get; set; }
    public bool Auditable { get; set; }
    public bool Archivable { get; set; }
    public List<FieldCreateDto> Fields { get; set; } = null!;
}
