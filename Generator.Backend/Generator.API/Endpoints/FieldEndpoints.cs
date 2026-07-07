using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Repository;
using Microsoft.AspNetCore.Mvc;
using static Generator.Domain.Core.Enums;

namespace Generator.API.Endpoints;

public static class FieldEndpoints
{
    public static void MapFieldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/fieldType/list/onbasetype", (FieldTypeRepository fieldTypeRepository) =>
        {
            var result = fieldTypeRepository.GetAll(filter: f => f.SourceTypeId == (byte)FieldTypeSourceEnums.Base);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/field/list/byEntity", (int entityId, FieldRepository fieldRepository) =>
        {
            var field = fieldRepository.GetAll(f => f.EntityId == entityId);
            if (field is null)
                return Results.NotFound();
            return Results.Ok(field);
        });

        app.MapGet("/field/list/updateModel", (int entityId, FieldRepository fieldRepository) =>
        {
            var field = fieldRepository.GetUpdateModels(entityId);
            if (field is null)
                return Results.NotFound();
            return Results.Ok(field);
        });

        app.MapPut("/field/list", ([FromQuery] int entityId, [FromBody] List<FieldUpdateDto> updateDtos, FieldRepository fieldRepository) =>
        {
            fieldRepository.Update(updateDtos, entityId);
            return Results.Ok();
        });
    }
}
