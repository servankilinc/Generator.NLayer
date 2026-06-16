using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Repository;

public class DtoFieldRepository : EFRepositoryBase<DtoField>
{
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
                    DtoFieldRelations = new List<DtoFieldRelationsCreateModel>()
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
                .Select(x => new DtoFieldRelationsCreateModel
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

    public void Add(DtoFieldCreateDto createDto)
    {
        using var context = new ProjectContext();
        var transaction = context.Database.BeginTransaction();
        try
        {
            HandleInsertDtoField(context, createDto);
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
            HandleUpdateDtoField(context, updateDto);

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
                    HandleInsertDtoField(context, new DtoFieldCreateDto
                    {
                        DtoId = dtoId,
                        Name = updateDto.Name,
                        SourceEntityId = updateDto.SourceEntityId,
                        SourceFieldId = updateDto.SourceFieldId,
                        IsList = updateDto.IsList,
                        IsRequired = updateDto.IsRequired,
                        DtoFieldRelations = updateDto.DtoFieldRelations,
                    });
                }
                else
                {
                    HandleUpdateDtoField(context, updateDto);
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


    #region Helpers
    private void HandleInsertDtoField(ProjectContext context, DtoFieldCreateDto createDto)
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
    }

    private void HandleUpdateDtoField(ProjectContext context, DtoFieldUpdateDto updateDto)
    {

        var existData = context.DtoFields.Include(i => i.Dto).FirstOrDefault(f => f.Id == updateDto.Id);

        if (existData == null)
            return;

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
    #endregion
}
