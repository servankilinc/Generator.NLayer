using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Dtos.Validation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using Generator.Domain.Core.Entities;

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



    //public void Add(DtoFieldCreateDto createDto)
    //{
    //    using var context = new ProjectContext();
    //    var transaction = context.Database.BeginTransaction();
    //    try
    //    {
    //        var dtoField = new DtoField
    //        {
    //            DtoId = createDto.DtoId,
    //            Name = createDto.Name,
    //            SourceFieldId = createDto.SourceFieldId,
    //            IsRequired = createDto.IsRequired,
    //            IsList = createDto.IsList
    //        };
    //        context.Add(dtoField);
    //        context.SaveChanges();

    //        if((createDto.DtoRelatedEntityId != createDto.SourceEntityId) && createDto.DtoFieldRelations != null)
    //        {
    //            var rangeOfRelations = createDto.DtoFieldRelations.Select(d => new DtoFieldRelations
    //            {
    //                DtoFieldId = dtoField.Id,
    //                RelationId = d.RelationId,
    //                SequenceNo = d.SequenceNo,
    //                Control = false
    //            });
    //            context.DtoFieldRelations.AddRange(rangeOfRelations);
    //            context.SaveChanges();
    //        }
    //        transaction.Commit();
    //    }
    //    catch (Exception)
    //    {
    //        transaction.Rollback();
    //    }
    //}

    //public void Update(DtoFieldUpdateDto updateDto)
    //{
    //    using var context = new ProjectContext();
    //    var transaction = context.Database.BeginTransaction();
    //    try
    //    {
    //        var existData = context.DtoFields.FirstOrDefault(f => f.Id == updateDto.Id);
    //        if (existData == null) throw new Exception("Data to update not found!");

    //        existData.Name = updateDto.Name;
    //        existData.SourceFieldId = updateDto.SourceFieldId;
    //        existData.IsRequired = updateDto.IsRequired;
    //        existData.IsList = updateDto.IsList;
    //        context.Update(existData);
    //        context.SaveChanges();

    //        if ((updateDto.DtoRelatedEntityId != updateDto.SourceEntityId) && updateDto.DtoFieldRelations != null)
    //        {
    //            var existDtoFieldRelations = context.DtoFieldRelations.Where(f => f.DtoFieldId == existData.Id);
    //            if (existDtoFieldRelations != null && existDtoFieldRelations.Any())
    //            {
    //                context.DtoFieldRelations.RemoveRange(existDtoFieldRelations);
    //                context.SaveChanges();
    //            }
                
    //            var rangeOfRelations = updateDto.DtoFieldRelations.Select(d => new DtoFieldRelations
    //            {
    //                DtoFieldId = existData.Id,
    //                RelationId = d.RelationId,
    //                SequenceNo = d.SequenceNo,
    //                Control = false
    //            });
    //            context.DtoFieldRelations.AddRange(rangeOfRelations);
    //            context.SaveChanges();
    //        }
    //        transaction.Commit();
    //    }
    //    catch (Exception)
    //    {
    //        transaction.Rollback();
    //    }
    //}

    public void Delete(int id)
    {
        using var context = new ProjectContext();
        var existData = context.DtoFields.FirstOrDefault(f => f.Id == id);

        if (existData == null) throw new Exception("Data not found!");

        context.Remove(existData);
        context.SaveChanges();
    }
}
