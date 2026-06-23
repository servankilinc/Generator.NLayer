using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace Generator.API.Endpoints;

public static class RelationEndpoints
{
    public static void MapRelationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/relationType/list", (RelationRepository relationRepository) =>
        {
            var result = relationRepository.GetRelationTypes();
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/deleteBehaviorType/list", (DeleteBehaviorTypeRepository deleteBehaviorTypeRepository) =>
        {
            var result = deleteBehaviorTypeRepository.GetAll();
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/relation/list", (RelationRepository relationRepository) =>
        {
            var result = relationRepository.GetAll(
                include: i => i
                    .Include(x => x.PrimaryField)
                        .ThenInclude(x => x.Entity)
                    .Include(x => x.ForeignField)
                        .ThenInclude(x => x.Entity)
                    .Include(x => x.RelationType)
                    .Include(x => x.DeleteBehaviorType)
            );

            if (result is null)
                return Results.NotFound();

            var data = result.Select(x => new RelationDetailModel
            {
                Id = x.Id,
                PrimaryEntityId = x.PrimaryField.EntityId,
                PrimaryEntityName = x.PrimaryField.Entity.Name,
                ForeignEntityId = x.ForeignField.EntityId,
                ForeignEntityName = x.ForeignField.Entity.Name,
                PrimaryFieldId = x.PrimaryFieldId,
                PrimaryFieldName = $"{x.PrimaryField.Entity.Name}.{x.PrimaryField.Name}",
                ForeignFieldId = x.ForeignFieldId,
                ForeignFieldName = $"{x.ForeignField.Entity.Name}.{x.ForeignField.Name}",
                RelationTypeId = x.RelationTypeId,
                RelationTypeName = x.RelationType.Name,
                DeleteBehaviorTypeId = x.DeleteBehaviorTypeId,
                DeleteBehaviorTypeName = x.DeleteBehaviorType.Name,
                PrimaryEntityVirPropName = x.PrimaryEntityVirPropName,
                ForeignEntityVirPropName = x.ForeignEntityVirPropName
            });
            return Results.Ok(data);
        });

        app.MapGet("/relation/list/byEntity", (int entityId, RelationRepository relationRepository) =>
        {
            var result = relationRepository.GetAll(
                filter: f => f.PrimaryField.EntityId == entityId || f.ForeignField.EntityId == entityId,
                include: i => i
                    .Include(x => x.PrimaryField)
                        .ThenInclude(x => x.Entity)
                    .Include(x => x.ForeignField)
                        .ThenInclude(x => x.Entity)
                    .Include(x => x.RelationType)
                    .Include(x => x.DeleteBehaviorType)
            );

            if (result is null)
                return Results.NotFound();

            var data = result.Select(x => new RelationDetailModel
            {
                Id = x.Id,
                PrimaryFieldId = x.PrimaryFieldId,
                PrimaryFieldName = $"{x.PrimaryField.Entity.Name}.{x.PrimaryField.Name}",
                ForeignFieldId = x.ForeignFieldId,
                ForeignFieldName = $"{x.ForeignField.Entity.Name}.{x.ForeignField.Name}",
                RelationTypeId = x.RelationTypeId,
                RelationTypeName = x.RelationType.Name,
                DeleteBehaviorTypeId = x.DeleteBehaviorTypeId,
                DeleteBehaviorTypeName = x.DeleteBehaviorType.Name,
                PrimaryEntityVirPropName = x.PrimaryEntityVirPropName,
                ForeignEntityVirPropName = x.ForeignEntityVirPropName
            });
            return Results.Ok(data);
        });

        app.MapGet("/relation/list/behindEntities", (int firstEntityId, int secondEntityId, RelationRepository relationRepository) =>
        {
            var result = relationRepository.GetRelationsBehindEntities(secondEntityId, firstEntityId)
                .Select(x => new RelationVisualModel
                {
                    Id = x.Id,
                    Name = x.PrimaryField.EntityId != firstEntityId ? $"(...).{x.ForeignEntityVirPropName}" : $"(...).{x.PrimaryEntityVirPropName}"
                });
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapPost("/relation", (RelationCreateDto createDto, RelationRepository relationRepository) =>
        {
            if (createDto.PrimaryFieldId == default || createDto.ForeignFieldId == default || createDto.RelationTypeId == default || createDto.DeleteBehaviorTypeId == default || string.IsNullOrEmpty(createDto.PrimaryEntityVirPropName) || string.IsNullOrEmpty(createDto.ForeignEntityVirPropName))
                return Results.BadRequest("Check The Fields!");
            relationRepository.AddRelation(createDto);
            return Results.Ok();
        });

        app.MapPut("/relation", (RelationUpdateDto updateDto, RelationRepository relationRepository) =>
        {
            if (updateDto.PrimaryFieldId == default || updateDto.ForeignFieldId == default || updateDto.RelationTypeId == default || updateDto.DeleteBehaviorTypeId == default || string.IsNullOrEmpty(updateDto.PrimaryEntityVirPropName) || string.IsNullOrEmpty(updateDto.ForeignEntityVirPropName))
                return Results.BadRequest("Check The Fields!");
            relationRepository.UpdateRelation(updateDto);
            return Results.Ok();
        });

        app.MapDelete("/relation", (int id, RelationRepository relationRepository) =>
        {
            var relation = relationRepository.Get(f => f.Id == id);
            if (relation is null)
                return Results.NotFound();
            relationRepository.Delete(relation);
            return Results.Ok();
        });
    }
}
