using Generator.Domain.Context;
using Generator.Domain.Core.Entities;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Repository.Base;

namespace Generator.Domain.Repository;

public class FieldRepository : EFRepositoryBase<Field>
{
    public List<FieldUpdateDto> GetUpdateModels(int entityId)
    {
        using var context = new ProjectContext();

        var existData = context.Fields.Where(f => f.EntityId == entityId).Select(f => new FieldUpdateDto
        {
            Id = f.Id,
            FieldTypeId = f.FieldTypeId,
            Name = f.Name,
            IsRequired = f.IsRequired,
            IsUnique = f.IsUnique,
            IsList = f.IsList,
            Filterable = f.Filterable
        }).ToList();
        return existData;
    }


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

    public void Update(List<FieldUpdateDto> updateDtos, int entityId)
    {
        using var context = new ProjectContext();

        foreach (var updateDto in updateDtos)
        {
            var existData = context.Fields.FirstOrDefault(f => f.Id == updateDto.Id);

            if (existData == null)
            {
                context.Fields.Add(new Field
                {
                    Id = updateDto.Id,
                    EntityId = entityId,
                    FieldTypeId = updateDto.FieldTypeId,
                    Name = updateDto.Name,
                    IsRequired = updateDto.IsRequired,
                    IsUnique = updateDto.IsUnique,
                    IsList = updateDto.IsList,
                    Filterable = updateDto.Filterable
                });
            }
            else
            {
                existData.FieldTypeId = updateDto.FieldTypeId;
                existData.Name = updateDto.Name;
                existData.IsRequired = updateDto.IsRequired;
                existData.IsUnique = updateDto.IsUnique;
                existData.IsList = updateDto.IsList;
                existData.Filterable = updateDto.Filterable;

                context.Fields.Update(existData);
            }
        }
        context.SaveChanges();
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
