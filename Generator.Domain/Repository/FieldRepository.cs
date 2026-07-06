using Generator.Domain.Context;
using Generator.Domain.Core.Entities;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Repository.Base;
using Generator.Domain.Core;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Repository;

public class FieldRepository : EFRepositoryBase<Field>
{
    public FieldRepository(ProjectContext context) : base(context)
    {
    }

    public List<FieldUpdateDto> GetUpdateModels(int entityId)
    {
        var existData = _context.Fields.Where(f => f.EntityId == entityId && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base).Include(i => i.FieldType).Select(f => new FieldUpdateDto
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

    public Field Add(FieldCreateDto fieldCreateDto)
    {
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
        _context.Set<Field>().Add(data);
        _context.SaveChanges();
        return data;
    }

    public Field Update(FieldUpdateDto updateDto)
    {
        var existData = _context.Fields.FirstOrDefault(f => f.Id == updateDto.Id);
        if (existData == null) throw new Exception("Data to update not found");

        existData.FieldTypeId = updateDto.FieldTypeId;
        existData.Name = updateDto.Name;
        existData.IsRequired = updateDto.IsRequired;
        existData.IsUnique = updateDto.IsUnique;
        existData.IsList = updateDto.IsList;
        existData.Filterable = updateDto.Filterable;

        _context.Fields.Update(existData);
        _context.SaveChanges();
        return existData;
    }

    public void Update(List<FieldUpdateDto> fieldsToUpdate, int entityId)
    {
        var transaction = _context.Database.BeginTransaction();

        try
        {
            var existFields = _context.Fields.Where(f => f.EntityId == entityId && f.FieldType.SourceTypeId == (byte)Enums.FieldTypeSourceEnums.Base).Include(i => i.FieldType);

            // delete not exist list
            foreach (var existField in existFields)
            {
                if (!fieldsToUpdate.Any(f => f.Id != default && f.Id == existField.Id))
                {
                    _context.Fields.Remove(existField);
                }
            }
            _context.SaveChanges();

            foreach (var updateDto in fieldsToUpdate)
            {
                var existData = existFields.FirstOrDefault(f => f.Id == updateDto.Id);

                if (existData == null)
                {
                    _context.Fields.Add(new Field
                    {
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

                    _context.Fields.Update(existData);
                }
            }
            _context.SaveChanges();

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
        }
    }
}
