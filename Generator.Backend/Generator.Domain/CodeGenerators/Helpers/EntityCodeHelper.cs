using System.Text;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Core;

namespace Generator.Domain.CodeGenerators.Helpers;

public static class EntityCodeHelper
{
    public static string GetConstraintRule(Entity entity)
    {
        return string.Join("/", entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).Select(f => $"{{{f.Name.ToCamelCase()}:{f.GetMapedTypeName().ToLower()}}}"));
    }

    public static string WhereRule(List<Field> uniqueFields, string? sourceName = null)
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

    public static string IncludeRule(Entity entity, Dto dto, DtoFieldRepository dtoFieldRepository)
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

    public static Field? GetSelectListTextField(Entity entity)
    {
        if (entity.Fields == default || entity.Fields.Any() == false) return default;

        Field? textField = entity.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant() == "name");
        if (textField == default)
        {
            textField = entity.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String && f.Name.Trim().ToLowerInvariant().Contains("name"));
        }
        if (textField == default)
        {
            textField = entity.Fields.FirstOrDefault(f => f.FieldTypeId == (byte)Enums.FieldTypeEnums.String);
        }

        return textField;
    }
}
