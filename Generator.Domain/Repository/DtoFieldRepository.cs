using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Core.Dtos.Validation;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Generator.Domain.Repository;

public class DtoFieldRepository : EFRepositoryBase<DtoField>
{
    public List<DtoField> GetBySourceField(Expression<Func<DtoField, bool>> expresion)
    {
        using var context = new ProjectContext();
        return context.DtoFields
                .Where(expresion)
                .Include(i => i.SourceField)
                    .ThenInclude(ti => ti.FieldType)
                .ToList();
    }

    public List<DtoFieldResponseDto> GetDetailList(Expression<Func<DtoField, bool>> expresion)
    {
        using var context = new ProjectContext();
        return context.DtoFields
                .Where(expresion)
                .Include(i => i.Dto)
                .Include(df => df.DtoFieldRelations)
                .Include(i => i.SourceField)
                    .ThenInclude(ti => ti.FieldType)
                .Include(i => i.SourceField)
                    .ThenInclude(ti => ti.Entity)
                .Select(y => new DtoFieldResponseDto
                {
                    Id = y.Id,
                    Name = y.Name,
                    DtoId = y.DtoId,
                    SourceFieldName = y.SourceField.Name,
                    EntityName = y.SourceField.Entity.Name,
                    FieldTypeName = y.SourceField.FieldType.Name,
                    IsRequired = y.IsRequired,
                    IsList = y.IsList,
                    IsSourceFromForeignEntity = y.Dto.RelatedEntityId != y.SourceField.EntityId,
                    IsThereRelations = y.DtoFieldRelations != null && y.DtoFieldRelations.Any(),
                    DtoFieldRelationsPath =
                            y.DtoFieldRelations != null ?
                                string.Join(",\n", y.DtoFieldRelations.OrderBy(o => o.SequenceNo)
                                    .Select(dr => $"{dr.Relation.PrimaryEntityVirPropName}.{dr.Relation.ForeignEntityVirPropName}")) :
                                string.Empty
                }).ToList();
    }


    public List<ValidationResponse> GetValidations(int dtoId)
    {
        using var context = new ProjectContext();
        var data = context.DtoFields
                .Where(f => f.DtoId == dtoId)
                .Include(i => i.SourceField)
                .Include(i => i.Validations)
                    .ThenInclude(i => i.ValidatorType)
                        .ThenInclude(i => i.ValidatorTypeParams)
                .Include(i => i.Validations)
                    .ThenInclude(i => i.ValidationParams)
                        .ThenInclude(i => i.ValidatorTypeParam)
                //.Select(d => d.Validations)
                .ToList();

        var validationList = new List<ValidationResponse>();
        if (data == null) return validationList;

        foreach (var item in data)
        {
            if (item.Validations != null)
            {
                validationList.AddRange(item.Validations.Select(x => new ValidationResponse
                {
                    ValidationId = x.Id,
                    DtoFieldId = item.Id,
                    ValidatorTypeName = x.ValidatorType.Name,
                    ErrorMessage = x.ErrorMessage,
                    ValidationParams = x.ValidationParams,
                    DtoId = item.DtoId,
                    SourceFieldName = item.SourceField.Name,
                    DtoFieldName = item.Name,
                }));
            }
        }
        return validationList;
    }


    public List<DtoFieldRelations> GetDtoFieldRelations(int dtoFieldId)
    {
        using var context = new ProjectContext();
        var data = context.DtoFieldRelations
                .Where(f => f.DtoFieldId == dtoFieldId)
                .Include(x => x.DtoField)
                    .ThenInclude(x => x.SourceField)
                        .ThenInclude(x => x.FieldType)
                .Include(x => x.Relation)
                    .ThenInclude(x => x.PrimaryField)
                        .ThenInclude(x => x.FieldType)
                .Include(x => x.Relation)
                    .ThenInclude(x => x.ForeignField)
                .OrderBy(o => o.SequenceNo)
                .ToList();
        return data;
    }

    public List<DtoFieldUpdateDto> GetUpdateDtos(int dtoId)
    {
        using var context = new ProjectContext();
        var result = new List<DtoFieldUpdateDto>();

        var dtoFields = context.DtoFields
                .Where(f => f.DtoId == dtoId)
                .Include(x => x.SourceField)
                .Include(x => x.Dto)
                .Select(dtoField => new DtoFieldUpdateDto
                {
                    Id = dtoField.Id,
                    //DtoRelatedEntityId = dtoField.Dto.RelatedEntityId,
                    SourceEntityId = dtoField.SourceField.EntityId,
                    Name = dtoField.Name,
                    SourceFieldId = dtoField.SourceFieldId,
                    IsRequired = dtoField.IsRequired,
                    IsList = dtoField.IsList,
                    DtoFieldRelations = new List<DtoFieldRelationsCreateForUpdateModel>()
                })
                .ToList();

        foreach (var dtoField in dtoFields)
        {
            var dtoFieldRelations = context.DtoFieldRelations
                .Where(f => f.DtoFieldId == dtoField.Id)
                .Include(x => x.DtoField)
                    .ThenInclude(x => x.SourceField)
                        //.ThenInclude(x => x.FieldType)
                .Include(x => x.DtoField)
                    .ThenInclude(x => x.Dto)
                //.Include(x => x.Relation)
                //    .ThenInclude(x => x.PrimaryField)
                //        .ThenInclude(x => x.FieldType)
                //.Include(x => x.Relation)
                //    .ThenInclude(x => x.ForeignField)
                .Select(x => new DtoFieldRelationsCreateForUpdateModel
                {
                    SequenceNo = x.SequenceNo,
                    FirstEntityId = x.DtoField.Dto.RelatedEntityId,
                    SecondEntityId = x.DtoField.SourceField.EntityId,
                    RelationId = x.RelationId
                })
                .OrderBy(o => o.SequenceNo)
                .ToList();

            if (dtoFieldRelations != null)
            {
                dtoField.DtoFieldRelations = dtoFieldRelations;
            }

            result.Add(dtoField);
        }

        return result;
    }


    public void Delete(int id)
    {
        using var context = new ProjectContext();
        var existData = context.DtoFields.FirstOrDefault(f => f.Id == id);

        if (existData == null) throw new Exception("Data not found!");

        context.Remove(existData);
        context.SaveChanges();
    }


    public void Add(DtoFieldCreateDto createDto)
    {
        using var context = new ProjectContext();
        var transaction = context.Database.BeginTransaction();
        try
        {
            var dtoField = new DtoField
            {
                DtoId = createDto.DtoId,
                Name = createDto.Name,
                SourceFieldId = createDto.SourceFieldId,
                IsRequired = createDto.IsRequired,
                IsList = createDto.IsList
            };
            context.Add(dtoField);
            context.SaveChanges();

            var dto = context.Dtos.First(f => f.Id == createDto.DtoId);

            if ((dto.RelatedEntityId != createDto.SourceEntityId) && createDto.DtoFieldRelations != null)
            {
                var rangeOfRelations = createDto.DtoFieldRelations.Select(d => new DtoFieldRelations
                {
                    DtoFieldId = dtoField.Id,
                    RelationId = d.RelationId,
                    SequenceNo = d.SequenceNo,
                    Control = false
                });
                context.DtoFieldRelations.AddRange(rangeOfRelations);
                context.SaveChanges();
            }
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
        }
    }

    public void Update(DtoFieldUpdateDto updateDto)
    {
        using var context = new ProjectContext();
        var transaction = context.Database.BeginTransaction();
        try
        {
            var existData = context.DtoFields.Include(i => i.Dto).FirstOrDefault(f => f.Id == updateDto.Id);
            if (existData == null) throw new Exception("Data to update not found!");

            existData.Name = updateDto.Name;
            existData.SourceFieldId = updateDto.SourceFieldId;
            existData.IsRequired = updateDto.IsRequired;
            existData.IsList = updateDto.IsList;
            context.Update(existData);
            context.SaveChanges();

            if ((existData.Dto.RelatedEntityId != updateDto.SourceEntityId) && updateDto.DtoFieldRelations != null)
            {
                var existDtoFieldRelations = context.DtoFieldRelations.Where(f => f.DtoFieldId == existData.Id);
                if (existDtoFieldRelations != null && existDtoFieldRelations.Any())
                {
                    context.DtoFieldRelations.RemoveRange(existDtoFieldRelations);
                    context.SaveChanges();
                }

                var rangeOfRelations = updateDto.DtoFieldRelations.Select(d => new DtoFieldRelations
                {
                    DtoFieldId = existData.Id,
                    RelationId = d.RelationId,
                    SequenceNo = d.SequenceNo,
                    Control = false
                });
                context.DtoFieldRelations.AddRange(rangeOfRelations);
                context.SaveChanges();
            }
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
        }
    }

    public void Update(List<DtoFieldUpdateDto> dtoFieldsToUpdate, int dtoId)
    {
        using var context = new ProjectContext();
        var transaction = context.Database.BeginTransaction();
        try
        {
            var existDtoFields = context.DtoFields.Where(f => f.DtoId == dtoId);

            // Delete DtoFields that are not in the updateDtos list
            foreach (var dtoField in existDtoFields)
            {
                if (!dtoFieldsToUpdate.Any(f => f.Id == dtoField.Id))
                {
                    context.DtoFields.Remove(dtoField);
                }
            }
            context.SaveChanges();


            foreach (var updateDto in dtoFieldsToUpdate)
            {
                var existData = existDtoFields.FirstOrDefault(f => f.Id == updateDto.Id);

                if (existData == null)
                {
                    var dtoField = new DtoField
                    {
                        DtoId = dtoId,
                        Name = updateDto.Name,
                        SourceFieldId = updateDto.SourceFieldId,
                        IsRequired = updateDto.IsRequired,
                        IsList = updateDto.IsList
                    };
                    context.Add(dtoField);
                    context.SaveChanges();

                    var dto = context.Dtos.First(f => f.Id == dtoId);

                    if ((dto.RelatedEntityId != updateDto.SourceEntityId) && updateDto.DtoFieldRelations != null)
                    {
                        var rangeOfRelations = updateDto.DtoFieldRelations.Select(d => new DtoFieldRelations
                        {
                            DtoFieldId = dtoField.Id,
                            RelationId = d.RelationId,
                            SequenceNo = d.SequenceNo,
                            Control = false
                        });
                        context.DtoFieldRelations.AddRange(rangeOfRelations);
                        context.SaveChanges();
                    }
                }
                else
                {
                    existData.Name = updateDto.Name;
                    existData.SourceFieldId = updateDto.SourceFieldId;
                    existData.IsRequired = updateDto.IsRequired;
                    existData.IsList = updateDto.IsList;
                    context.DtoFields.Update(existData);
                    context.SaveChanges();

                    var srcField = context.Fields.First(f => f.Id == updateDto.SourceFieldId);

                    if ((existData.Dto.RelatedEntityId != srcField.EntityId) && updateDto.DtoFieldRelations != null)
                    {
                        var existDtoFieldRelations = context.DtoFieldRelations.Where(f => f.DtoFieldId == existData.Id);
                        if (existDtoFieldRelations != null && existDtoFieldRelations.Any())
                        {
                            context.DtoFieldRelations.RemoveRange(existDtoFieldRelations);
                            context.SaveChanges();
                        }

                        var rangeOfRelations = updateDto.DtoFieldRelations.Select(d => new DtoFieldRelations
                        {
                            DtoFieldId = existData.Id,
                            RelationId = d.RelationId,
                            SequenceNo = d.SequenceNo,
                            Control = false
                        });
                        context.DtoFieldRelations.AddRange(rangeOfRelations);
                        context.SaveChanges();
                    }
                }
            }
            context.SaveChanges();

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
        }
    }
}
