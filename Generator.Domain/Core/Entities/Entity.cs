using Generator.Domain.Core.Entities.Base;
using Generator.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Generator.Domain.Core.Entities;

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
    public virtual AppSetting? AsUserAppSetting { get; set; }
    public virtual AppSetting? AsRoleAppSetting { get; set; }


    #region Helpers
    public string GetUniqueArgs()
    {
        return String.Join(", ", Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).Select(f => $"{f.GetMapedTypeName()} {f.Name.ToCamelCase()}"));
    }

    public string GetConstraintRule()
    {
        // ex: return {blogId:guid}/{userId:guid}
        return String.Join("/", Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).Select(f => $"{{{f.Name.ToCamelCase()}:{f.GetMapedTypeName().ToLower()}}}"));
    }

    public string WhereRule(List<Field> uniqueFields, string? sourceName = null)
    {
        bool hasSource = !string.IsNullOrWhiteSpace(sourceName);
        sourceName = hasSource ? sourceName!.Trim() : "";

        var conditions = uniqueFields.OrderBy(f => f.Name).Select(f =>
        {
            var right = hasSource ? $"{sourceName}.{f.Name}" : f.Name.ToCamelCase();
            return $"f.{f.Name} == {right}";
        });

        return $"where: (f) => {string.Join(" && ", conditions)}";
    }

    public string IncludeRule(Dto dto, DtoFieldRepository dtoFieldRepository)
    {
        var sb = new StringBuilder();
        bool firstIncludeWritten = false;

        var dto_DtoFields = dtoFieldRepository.GetAll(f => f.DtoId == dto.Id, include: i => i.Include(x => x.SourceField));

        if (dto_DtoFields != default)
            dto_DtoFields = dto_DtoFields
                .GroupBy(p => p.SourceField.EntityId)
                .Select(g => g.First())
                .ToList();

        foreach (var dtoField in dto_DtoFields!)
        {
            var dtoFieldRelations = dtoFieldRepository.GetDtoFieldRelations(dtoField.Id);
            if (!dtoFieldRelations.Any()) continue;

            var dfrFirst = dtoFieldRelations.First();

            bool controlOfRelationFirst = dfrFirst.Relation.PrimaryField.EntityId == dto.RelatedEntityId;

            string destPropOfFirst = controlOfRelationFirst ? dfrFirst.Relation.PrimaryEntityVirPropName : dfrFirst.Relation.ForeignEntityVirPropName;

            if (!firstIncludeWritten)
            {
                sb.Append($"i.Include(x => x.{destPropOfFirst})");
                firstIncludeWritten = true;
            }
            else
            {
                sb.Append($".Include(x => x.{destPropOfFirst})");
            }

            int lastDestEntityId = controlOfRelationFirst ? dfrFirst.Relation.ForeignField.EntityId : dfrFirst.Relation.PrimaryField.EntityId;

            for (int i = 1; i < dtoFieldRelations.Count; i++)
            {
                var dfr = dtoFieldRelations[i];

                bool controlOfRelation = dfr.Relation.PrimaryField.EntityId == lastDestEntityId;

                string destProp = controlOfRelation ? dfr.Relation.PrimaryEntityVirPropName : dfr.Relation.ForeignEntityVirPropName;

                sb.Append($".ThenInclude(x => x.{destProp})");

                lastDestEntityId = controlOfRelation ? dfr.Relation.ForeignField.EntityId : dfr.Relation.PrimaryField.EntityId;
            }
        }

        if (sb.Length == 0)
            return "include: null";

        return $"include: i => {sb}";
    }

    public Field? GetSelectListTextField()
    {
        if (this.Fields == default || this.Fields.Any() == false) return default;

        Field? textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant() == "name");
        if (textField == default)
        {
            textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant().Contains("name"));
        }
        if (textField == default)
        {
            textField = this.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String);
        }

        return textField;
    }
    #endregion
}
