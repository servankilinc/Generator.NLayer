using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Generator.API.Endpoints;

public static class DtoEndpoints
{
    public static void MapDtoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/crudtype/list", (CrudTypeRepository crudTypeRepository) =>
        {
            var result = crudTypeRepository.GetAll(enableTracking: false);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/dto/updateModel", (int dtoId, DtoRepository dtoRepository) =>
        {
            var result = dtoRepository.GetUpdateModel(dtoId);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/dto/list/byEntity", (int entityId, DtoRepository dtoRepository) =>
        {
            var result = dtoRepository.GetAll(filter: f => f.RelatedEntityId == entityId, include: i => i.Include(x => x.CrudType), enableTracking: false);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/dto/list/detail", (int entityId, DtoRepository dtoRepository) =>
        {
            var result = dtoRepository.GetDetailList(f => f.RelatedEntityId == entityId);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapPost("/dto", (DtoCreateDto createDto, DtoRepository dtoRepository) =>
        {
            if (string.IsNullOrEmpty(createDto.Name) || createDto.RelatedEntityId == default || createDto.CrudTypeId == default)
                return Results.BadRequest("Check The Fields!");

            dtoRepository.CreateByFields(createDto);
            return Results.Ok();
        });

        app.MapPut("/dto", (DtoUpdateDto updateDto, DtoRepository dtoRepository) =>
        {
            if (string.IsNullOrEmpty(updateDto.Name) || updateDto.RelatedEntityId == default || updateDto.CrudTypeId == default || updateDto.Id == default)
                return Results.BadRequest("Check The Fields!");
            dtoRepository.Update(updateDto);
            return Results.Ok();
        });

        app.MapDelete("/dto", (int id, DtoRepository dtoRepository) =>
        {
            dtoRepository.Delete(id);
            return Results.Ok();
        });

        app.MapGet("/dtofield/list/updateModel", (int dtoId, DtoFieldRepository dtoFieldRepository) =>
        {
            var dtofields = dtoFieldRepository.GetUpdateDtos(dtoId);
            if (dtofields is null)
                return Results.NotFound();
            return Results.Ok(dtofields);
        });

        app.MapPut("/dtofield/list", ([FromQuery] int dtoId, [FromBody] List<DtoFieldUpdateDto> updateDtos, DtoFieldRepository dtoFieldRepository) =>
        {
            dtoFieldRepository.Update(updateDtos, dtoId);
            return Results.Ok();
        });
    }
}
