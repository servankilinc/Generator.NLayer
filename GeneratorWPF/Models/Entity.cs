using GeneratorWPF.Extensions;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Models.Signature;

namespace GeneratorWPF.Models
{
    public class Entity : EntityBase
    {
        public int Id { get; set; }
        public string TableName { get; set; } = null!;
        public string Name { get; set; } = null!;

        public int? CreateDtoId { get; set; }
        public int? UpdateDtoId { get; set; }
        public int? DeleteDtoId { get; set; }
        public int? ReportDtoId { get; set; }
        public int? BasicResponseDtoId { get; set; }
        public int? DetailResponseDtoId { get; set; }

        public bool SoftDeletable { get; set; }
        public bool Auditable { get; set; }
        public bool Archivable { get; set; }

        public virtual Dto? CreateDto { get; set; }
        public virtual Dto? UpdateDto { get; set; }
        public virtual Dto? DeleteDto { get; set; }
        public virtual Dto? ReportDto { get; set; }
        public virtual Dto? BasicResponseDto { get; set; }
        public virtual Dto? DetailResponseDto { get; set; }
        public virtual ICollection<Field> Fields { get; set; } = null!;
        public virtual ICollection<Dto>? Dtos { get; set; }
        public virtual ICollection<Service>? Services { get; set; }
        public virtual AppSetting? AsUserAppSetting { get; set; }
        public virtual AppSetting? AsRoleAppSetting { get; set; }


        public string GetUniqueArgs()
        {
            return String.Join(", ", Fields.Where(f => f.IsUnique).Select(f => $"{f.GetMapedTypeName()} {f.Name.ToCamelCase()}").ToArray());
        }

        public string WhereRule(List<Field> uniqueFields, string? sourceName = null)
        {
            bool hasSource = !string.IsNullOrWhiteSpace(sourceName);
            sourceName = hasSource ? sourceName!.Trim() : "";

            var conditions = uniqueFields.Select(f =>
            {
                var right = hasSource ? $"{sourceName}.{f.Name}" : f.Name.ToCamelCase();
                return $"f.{f.Name} == {right}";
            });

            return $"where: (f) => {string.Join(" && ", conditions)}";
        }

        public Field? GetSelectListTextField()
        {
            if (this.Fields == default || this.Fields.Any() == false) return default;

            Field? textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant() == "name");
            if (textField == default)
            {
                textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant().Contains("name"));
            }
            if (textField == default)
            {
                textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)FieldTypeEnums.String);
            }

            return textField;
        }
    }
}
