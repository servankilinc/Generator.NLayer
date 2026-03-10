using GeneratorWPF.Dtos._Field;
using System.Collections.ObjectModel;

namespace GeneratorWPF.Dtos._Entity
{
    public class EntityCreateDto
    {
        public string Name { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public bool SoftDeletable { get; set; }
        public bool Auditable { get; set; }
        public bool Archivable { get; set; }
        public List<FieldCreateDto> Fields { get; set; } = null!;
    }
}
