using Generator.Domain.Context;
using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;

namespace Generator.Domain.Repository;

public class EntityRepository : EFRepositoryBase<Entity>
{
    public void Create(EntityCreateDto createDto)
    {
        using var _context = new ProjectContext();
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Add Entity
            var entityToInsert = new Entity
            {
                Name = createDto.Name,
                TableName = createDto.TableName,
                SoftDeletable = createDto.SoftDeletable,
                Auditable = createDto.Auditable,
                Archivable = createDto.Archivable
            };
            var insertedEntity = _context.Set<Entity>().Add(entityToInsert).Entity;
            _context.SaveChanges();

            // Add Fields 
            foreach (var fieldCreateDto in createDto.Fields)
            {
                _context.Set<Field>().Add(new Field
                {
                    FieldTypeId = fieldCreateDto.FieldTypeId,
                    EntityId = insertedEntity.Id,
                    Name = fieldCreateDto.Name,
                    IsRequired = fieldCreateDto.IsRequired,
                    IsUnique = fieldCreateDto.IsUnique,
                    IsList = fieldCreateDto.IsList,
                    Filterable = fieldCreateDto.Filterable
                });
            }
            _context.SaveChanges();

            // Add FieldType
            _context.Set<FieldType>().Add(new FieldType
            {
                Name = insertedEntity.Name,
                SourceTypeId = (int)Enums.FieldTypeSourceEnums.Entity,
            });
            _context.SaveChanges();

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public EntityUpdateDto GetUpdateModel(int entityId)
    {
        using var context = new ProjectContext();

        var existEntity = context.Entities
            .Select(e => new EntityUpdateDto
            {
                Id = e.Id,
                Name = e.Name,
                TableName = e.TableName,
                SoftDeletable = e.SoftDeletable,
                Auditable = e.Auditable,
                Archivable = e.Archivable,
                CreateDtoId = e.CreateDtoId,
                UpdateDtoId = e.UpdateDtoId,
                DeleteDtoId = e.DeleteDtoId,
                ReportDtoId = e.ReportDtoId,
                BasicResponseDtoId = e.BasicResponseDtoId,
                DetailResponseDtoId = e.DetailResponseDtoId
            })
            .FirstOrDefault(f => f.Id == entityId);
        if (existEntity == null)
            throw new Exception("Data to update not found");

        return existEntity;
    }

    public Entity Update(EntityUpdateDto updateDto)
    {
        using var context = new ProjectContext();

        var existData = context.Entities.FirstOrDefault(f => f.Id == updateDto.Id);
        if (existData == null) throw new Exception("Data to update not found");

        existData.Name = updateDto.Name;
        existData.TableName = updateDto.TableName;

        existData.CreateDtoId = updateDto.CreateDtoId;
        existData.UpdateDtoId = updateDto.UpdateDtoId;
        existData.DeleteDtoId = updateDto.DeleteDtoId;
        existData.ReportDtoId = updateDto.ReportDtoId;
        existData.BasicResponseDtoId = updateDto.BasicResponseDtoId;
        existData.DetailResponseDtoId = updateDto.DetailResponseDtoId;

        existData.SoftDeletable = updateDto.SoftDeletable;
        existData.Auditable = updateDto.Auditable;
        existData.Archivable = updateDto.Archivable;

        context.Entities.Update(existData);
        context.SaveChanges();
        return existData;
    }

    public void Delete(int id)
    {
        using var context = new ProjectContext();

        var existData = context.Entities.FirstOrDefault(f => f.Id == id);
        if (existData == null) throw new Exception("Data to delete not found");

        context.Entities.Remove(existData);
        context.SaveChanges();
    }
}
