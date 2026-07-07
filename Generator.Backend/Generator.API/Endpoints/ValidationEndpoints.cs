using Generator.Domain.Core.Dtos.Validation;
using Generator.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace Generator.API.Endpoints;

public static class ValidationEndpoints
{
    public static void MapValidationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/validatorType/list", (ValidatorTypeRepository validatorTypeRepository) =>
        {
            var result = validatorTypeRepository.GetAll(include: i => i.Include(x => x.ValidatorTypeParams), enableTracking: false);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/validatorTypeParam/list", (int validatorTypeId, ValidatorTypeParamRepository validatorTypeParamRepository) =>
        {
            var result = validatorTypeParamRepository.GetAll(filter: f => f.ValidatorTypeId == validatorTypeId, include: i => i.Include(i => i.ValidatorType));
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapGet("/validation/list/updateModel", (int dtoFieldId, ValidationRepository validationRepository) =>
        {
            var result = validationRepository.GetUpdateDtos(dtoFieldId);
            return Results.Ok(result);
        });

        app.MapPost("/validation/list", (List<ValidationUpdateDto> updateDtos, ValidationRepository validationRepository) =>
        {
            if (
                updateDtos.Any(f => f.ValidatorTypeId == default) ||
                updateDtos.Any(f => f.DtoFieldId == default) ||
                updateDtos.DistinctBy(f => f.DtoFieldId).ToList().Count > 1 ||
                updateDtos.Any(f => f.ValidationParams != null && f.ValidationParams.Any(fi => string.IsNullOrEmpty(fi.Value)))
            )
                return Results.BadRequest("Check The Fields!");

            validationRepository.Update(updateDtos);
            return Results.Ok();
        });
    }
}
