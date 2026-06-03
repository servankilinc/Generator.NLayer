using Generator.Domain.Context;
using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Generator.Domain.Repository;

public class DtoRepository : EFRepositoryBase<Dto>
{
    public DtoUpdateDto GetUpdateModel(int entityId)
    {
        using var context = new ProjectContext();

        var existEntity = context.Dtos
            .Select(e => new DtoUpdateDto
            {
                Id = e.Id,
                Name = e.Name,
                CrudTypeId = e.CrudTypeId,
                RelatedEntityId = e.RelatedEntityId
            })
            .FirstOrDefault(f => f.Id == entityId);
        if (existEntity == null)
            throw new Exception("Data to update not found");

        return existEntity;
    }

    public DtoField GetDtoFieldByValidations(Expression<Func<DtoField, bool>> expresion)
    {
        using var context = new ProjectContext();
        return context.DtoFields
                .Where(expresion)
                .Include(i => i.Validations)
                    .ThenInclude(i => i.ValidatorType)
                        .ThenInclude(i => i.ValidatorTypeParams)
                .Include(i => i.Validations)
                    .ThenInclude(i => i.ValidationParams)
                        .ThenInclude(i => i.ValidatorTypeParam)
                .FirstOrDefault();
    }


    public List<Dto> GetListByValidations(Expression<Func<Dto, bool>> expresion)
    {
        using var context = new ProjectContext();
        return context.Dtos
                .Where(expresion)
                .Include(i => i.RelatedEntity)
                .Include(i => i.DtoFields)
                    .ThenInclude(df => df.Validations)
                        .ThenInclude(v => v.ValidatorType)
                .Include(i => i.DtoFields)
                    .ThenInclude(df => df.Validations)
                        .ThenInclude(v => v.ValidationParams)
                            .ThenInclude(vp => vp.ValidatorTypeParam)
                .ToList();
    }


    public List<Dto> GetList(Expression<Func<Dto, bool>> expresion)
    {
        using var context = new ProjectContext();
        return context.Dtos
                .Where(expresion)
                .Include(i => i.RelatedEntity)
                .Include(i => i.CrudType)
                .Include(i => i.DtoFields)
                    .ThenInclude(df => df.SourceField)
                        .ThenInclude(sf => sf.FieldType)
                .Include(i => i.DtoFields)
                    .ThenInclude(df => df.SourceField)
                        .ThenInclude(sf => sf.Entity)
                .ToList();
    }

    public List<DtoDetailResponseDto> GetDetailList(Expression<Func<Dto, bool>> expresion)
    {
        using var context = new ProjectContext();
        //return context.Dtos
        //        .Where(expresion)
        //        .Include(i => i.RelatedEntity)
        //        .Include(i => i.CrudType)
        //        .Include(i => i.DtoFields)
        //            .ThenInclude(df => df.SourceField)
        //                .ThenInclude(sf => sf.FieldType)
        //        .Include(i => i.DtoFields)
        //            .ThenInclude(df => df.SourceField)
        //                .ThenInclude(sf => sf.Entity)
        //        .Include(i => i.DtoFields)
        //            .ThenInclude(df => df.DtoFieldRelations)
        //        .Select(x => new DtoDetailResponseDto
        //        {
        //            Id = x.Id,
        //            Name = x.Name,
        //            RelatedEntityName = x.RelatedEntity.Name,
        //            CrudTypeName = x.CrudType.Name,
        //            DtoFields = x.DtoFields.Select(y => new DtoFieldResponseDto
        //            {
        //                Id = y.Id,
        //                Name = y.Name,
        //                DtoId = x.Id,
        //                SourceFieldName = y.SourceField.Name,
        //                EntityName = y.SourceField.Entity.Name,
        //                FieldTypeName = y.SourceField.FieldType.Name,
        //                IsRequired = y.IsRequired,
        //                IsList = y.IsList,
        //                IsSourceFromForeignEntity = x.RelatedEntityId != y.SourceField.EntityId,
        //                IsThereRelations = y.DtoFieldRelations != null && y.DtoFieldRelations.Any(),
        //                DtoFieldRelationsPath =
        //                    y.DtoFieldRelations != null ?
        //                        string.Join(",\n", y.DtoFieldRelations.OrderBy(o => o.SequenceNo)
        //                            .Select(dr => $"{dr.Relation.PrimaryEntityVirPropName}.{dr.Relation.ForeignEntityVirPropName}")) :
        //                        string.Empty
        //            }).ToList()
        //        }).ToList();

        var data = context.Dtos
            .Where(expresion)
            .Where(expresion)
            .Include(i => i.RelatedEntity)
            .Include(i => i.CrudType)
            .Include(i => i.DtoFields)
                .ThenInclude(df => df.SourceField)
                    .ThenInclude(sf => sf.FieldType)
            .Include(i => i.DtoFields)
                .ThenInclude(df => df.SourceField)
                    .ThenInclude(sf => sf.Entity)
            .Include(i => i.DtoFields)
                .ThenInclude(df => df.DtoFieldRelations)
            .AsNoTracking()
            .ToList();

        // Bellekte LINQ ile dönüştürme
        return data.Select(x => new DtoDetailResponseDto
        {
            Id = x.Id,
            Name = x.Name,
            RelatedEntityName = x.RelatedEntity?.Name,
            CrudTypeName = x.CrudType?.Name,
            DtoFields = x.DtoFields.Select(y => new DtoFieldResponseDto
            {
                Id = y.Id,
                Name = y.Name,
                DtoId = x.Id,
                SourceFieldName = y.SourceField.Name,
                EntityName = y.SourceField.Entity.Name,
                FieldTypeName = y.SourceField.FieldType.Name,
                IsRequired = y.IsRequired,
                IsList = y.IsList,
                IsSourceFromForeignEntity = x.RelatedEntityId != y.SourceField.EntityId,
                IsThereRelations = y.DtoFieldRelations != null && y.DtoFieldRelations.Any(),
                DtoFieldRelationsPath =
                    y.DtoFieldRelations != null ?
                        string.Join(",\n", y.DtoFieldRelations.OrderBy(o => o.SequenceNo)
                            .Select(dr => $"{dr.Relation.PrimaryEntityVirPropName}.{dr.Relation.ForeignEntityVirPropName}")) :
                        string.Empty
            }).ToList()
        }).ToList();
    }

    public void CreateByFields(DtoCreateDto createDto)
    {
        using var _context = new ProjectContext();
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Insert Dto
            var insertedDto = _context.Dtos.Add(new Dto()
            {
                Name = createDto.Name,
                RelatedEntityId = createDto.RelatedEntityId,
                CrudTypeId = createDto.CrudTypeId,
            }).Entity;
            _context.SaveChanges();

            // Insert Dto as FiledType (Dto ismi güncellendiğinde bu kaydın ismi de güncellenmeli) // add like variable int, string ...
            var insertedFieldType = _context.FieldTypes.Add(new FieldType
            {
                Name = insertedDto.Name,
                SourceTypeId = (int)Enums.FieldTypeSourceEnums.Dto,
            }).Entity;
            _context.SaveChanges();

            // Insert Field (Dto EntityId ve Name güncellendiğinde bu kayıt güncellenmeli) // ad like props name, age ...
            var insertedField = _context.Fields.Add(new Field()
            {
                EntityId = createDto.RelatedEntityId,
                FieldTypeId = insertedFieldType.Id,
                Name = createDto.Name,
            }).Entity;
            _context.SaveChanges();

            // Insert DtoFields
            if (createDto.DtoFields != null && createDto.DtoFields.Any())
            {
                foreach (var sourceField in createDto.DtoFields)
                {
                    var dtoField = _context.DtoFields.Add(new DtoField
                    {
                        DtoId = insertedDto.Id,
                        SourceFieldId = sourceField.SourceFieldId,
                        Name = sourceField.Name,
                        IsRequired = sourceField.IsRequired,
                        IsList = sourceField.IsList,
                        DtoFieldRelations = (insertedDto.RelatedEntityId != sourceField.SourceEntityId) && sourceField.DtoFieldRelations != null && sourceField.DtoFieldRelations.Any() ?
                            sourceField.DtoFieldRelations.Select(d => new DtoFieldRelations
                            {
                                RelationId = d.RelationId,
                                SequenceNo = d.SequenceNo,
                                Control = false
                            }).ToList() : null
                    });
                }
                _context.SaveChanges();

            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Update(DtoUpdateDto updateModel)
    {
        using var context = new ProjectContext();
        var existData = context.Dtos.FirstOrDefault(f => f.Id == updateModel.Id);
        if (existData == null) throw new Exception("Data to update not found!");

        bool nameChanged = existData.Name != updateModel.Name;
        bool entityIdChanged = existData.RelatedEntityId != updateModel.RelatedEntityId;

        FieldType? existFieldType = context.FieldTypes.FirstOrDefault(f => f.Name == existData.Name && f.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Dto);
        Field? existField = context.Fields.FirstOrDefault(f => f.Name == existData.Name && f.EntityId == existData.RelatedEntityId);
        if (nameChanged)
        {
            if (existFieldType != null) existFieldType.Name = updateModel.Name;
            if (existField != null) existField.Name = updateModel.Name;

            existData.Name = updateModel.Name;
        }
        if (entityIdChanged)
        {
            if (existField != null) existField.EntityId = updateModel.RelatedEntityId;

            existData.RelatedEntityId = updateModel.RelatedEntityId;
        }

        if (existField != null) context.Update(existField);
        if (existFieldType != null) context.Update(existFieldType);

        existData.CrudTypeId = updateModel.CrudTypeId;

        context.Update(existData);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new ProjectContext();

        Dto? dto = context.Dtos.FirstOrDefault(f => f.Id == id);

        if (dto == null) throw new Exception("Data not found!");

        FieldType? fieldType = context.FieldTypes.FirstOrDefault(f => f.Name == dto.Name && f.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Dto);
        Field? field = context.Fields.FirstOrDefault(f => f.Name == dto.Name && f.EntityId == dto.RelatedEntityId);

        if (fieldType == null || field == null) throw new Exception("Related Data(s) not found!");

        context.Remove(dto);
        context.Remove(field);
        context.Remove(fieldType);

        context.SaveChanges();
    }
}
