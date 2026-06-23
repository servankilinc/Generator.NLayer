using Generator.Domain.Core.Dtos.AppSetting;
using Generator.Domain.Repository;

namespace Generator.API.Endpoints;

public static class AppSettingEndpoints
{
    public static void MapAppSettingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/appSetting", (AppSettingsRepository appSettingsRepository) =>
        {
            var result = appSettingsRepository.Get(f => f.Id == 1);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        app.MapPut("/appSetting", (AppSettingUpdateDto updateDto, AppSettingsRepository appSettingsRepository) =>
        {
            bool checkUser = !updateDto.IsThereUser || updateDto.UserEntityId != default;
            bool checkRole = !updateDto.IsThereRole || updateDto.RoleEntityId != default;
            if (!checkRole || !checkUser)
                return Results.BadRequest("Check The Fields!");

            var appSetting = appSettingsRepository.Get(f => f.Id == 1);
            if (appSetting is null)
                return Results.NotFound();

            updateDto.MapToEntity(appSetting);
            appSettingsRepository.Update(appSetting);
            return Results.Ok(updateDto);
        });
    }
}
