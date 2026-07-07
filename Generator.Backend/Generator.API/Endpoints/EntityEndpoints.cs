using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Generator.API.Endpoints;

public static class EntityEndpoints
{
    public static void MapEntityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/entity/updateModel", (int entityId, EntityRepository entityRepository) =>
        {
            var result = entityRepository.GetUpdateModel(entityId);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/entity/list/withBaseFields", (EntityRepository entityRepository) =>
        {
            var result = entityRepository.GetAll(
                include: i => i
                    .Include(e => e.CreateDto)
                    .Include(e => e.UpdateDto)
                    .Include(e => e.DeleteDto)
                    .Include(e => e.ReportDto)
                    .Include(e => e.BasicResponseDto)
                    .Include(e => e.DetailResponseDto)
                    .Include(e => e.Fields.Where(f => f.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base))
                        .ThenInclude(f => f.FieldType),
                enableTracking: false
            );
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapPost("/entity", (EntityCreateDto createDto, EntityRepository entityRepository) =>
        {
            if (string.IsNullOrEmpty(createDto.Name) || string.IsNullOrEmpty(createDto.TableName) || createDto.Fields.Count == 0)
                return Results.BadRequest("Check The Fields!");

            entityRepository.Create(createDto);
            return Results.Ok();
        });

        app.MapPut("/entity", (EntityUpdateDto updateDto, EntityRepository entityRepository) =>
        {
            if (string.IsNullOrEmpty(updateDto.Name) || string.IsNullOrEmpty(updateDto.TableName))
                return Results.BadRequest("Check The Fields!");

            entityRepository.Update(updateDto);
            return Results.Ok();
        });

        app.MapDelete("/entity", (int id, EntityRepository entityRepository) =>
        {
            entityRepository.Delete(id);
            return Results.Ok();
        });
    }
}
