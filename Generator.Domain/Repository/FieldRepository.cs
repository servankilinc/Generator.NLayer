using Generator.Domain.Context;
using Generator.Domain.Dtos.Field;
using Generator.Domain.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Repository;

public class FieldRepository : EFRepositoryBase<Field>
{
    public Field Update(FieldUpdateDto updateDto)
    {
        using var context = new ProjectContext();

        var existData = context.Fields.FirstOrDefault(f => f.Id == updateDto.Id);
        if (existData == null) throw new Exception("Data to update not found");
          
        existData.FieldTypeId = updateDto.FieldTypeId;
        existData.Name = updateDto.Name;
        existData.IsRequired = updateDto.IsRequired;
        existData.IsUnique = updateDto.IsUnique;
        existData.IsList = updateDto.IsList;
        existData.Filterable = updateDto.Filterable;

        context.Fields.Update(existData);
        context.SaveChanges();
        return existData;
    }

    public Field Add(FieldCreateDto fieldCreateDto)
    {
        using var context = new ProjectContext();

        var data = new Field
        {
            FieldTypeId = fieldCreateDto.FieldTypeId,
            EntityId = fieldCreateDto.EntityId,
            Name = fieldCreateDto.Name,
            IsRequired = fieldCreateDto.IsRequired,
            IsUnique = fieldCreateDto.IsUnique,
            IsList = fieldCreateDto.IsList,
            Filterable = fieldCreateDto.Filterable
        };
        context.Set<Field>().Add(data);
        context.SaveChanges();
        return data;
    }

    public string GetFieldName(int fieldId)
    {
        using var context = new ProjectContext();

        var name = context.Fields.Where(f => f.Id == fieldId).Select(d => d.Name).FirstOrDefault();

        return name;
    }
}
