using Generator.Domain.Context;
using Generator.Domain.Core;
using Generator.Domain.Core.Dtos.AppSetting;
using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Core.Dtos.Validation;
using Generator.Domain.Core.Entities.Local;
using Generator.Domain.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using static Generator.Domain.Core.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("allow-policy",
        policy =>
        {
            policy
                .AllowAnyOrigin() //.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi();

builder.Services.AddScoped<AppSettingsRepository>();
builder.Services.AddScoped<CrudTypeRepository>();
builder.Services.AddScoped<DeleteBehaviorTypeRepository>();
builder.Services.AddScoped<DtoFieldRelationsRepository>();
builder.Services.AddScoped<DtoFieldRepository>();
builder.Services.AddScoped<DtoRepository>();
builder.Services.AddScoped<EntityRepository>();
builder.Services.AddScoped<FieldRepository>();
builder.Services.AddScoped<FieldTypeRepository>();
builder.Services.AddScoped<RelationRepository>();
builder.Services.AddScoped<ValidationRepository>();


var app = builder.Build();

app.UseCors("allow-policy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();


#region Project
//ok
app.MapGet("/activeProject", () =>
{
    try
    {
        if (Statics.CurrentProject is null)
            return Results.NotFound();
        return Results.Ok(Statics.CurrentProject);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapPost("/activeProject", (int id) =>
{
    try
    {
        using var localContext = new LocalContext();

        Statics.CurrentProject = localContext.Projects.FirstOrDefault(x => x.Id == id);

        if (Statics.CurrentProject is null)
            return Results.NotFound();
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapGet("/project/list", async () =>
{
    try
    {
        using var localContext = new LocalContext();
        var result = await localContext.Projects.ToListAsync();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapPost("/project", async (Project project) =>
{
    try
    {
        using var localContext = new LocalContext();

        project.CreateDate = DateTime.Now;
        await localContext.Projects.AddAsync(project);
        await localContext.SaveChangesAsync();

        return Results.Ok(project);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapDelete("/project", async (int id) =>
{
    try
    {
        using var localContext = new LocalContext();

        var project = localContext.Projects.FirstOrDefault(x => x.Id == id);

        if (project is null)
            return Results.NotFound();

        localContext.Projects.Remove(project);
        await localContext.SaveChangesAsync();

        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region AppSettings
// ok
app.MapGet("/appSetting", (AppSettingsRepository appSettingsRepository) =>
{
    try
    {
        var result = appSettingsRepository.Get(f => f.Id == 1);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError("Don't forget to make sure you have selected a project.");
    }
});

// ok
app.MapPut("/appSetting", (AppSettingUpdateDto updateDto, AppSettingsRepository appSettingsRepository) =>
{
    try
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
    }
    catch (Exception)
    {
        return Results.InternalServerError("Don't forget to make sure you have selected a project.");
    }
});
#endregion

#region Entity
app.MapGet("/entity", (int id, EntityRepository entityRepository) =>
{
    try
    {
        var result = entityRepository.Get(filter: f => f.Id == id);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/entity/withBaseFields", (int id, EntityRepository entityRepository) =>
{
    try
    {
        var result = entityRepository.Get(filter: f => f.Id == id, include: i => i.Include(e => e.Fields.Where(f => f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)).ThenInclude(f => f.FieldType));
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapGet("/entity/updateModel", (int entityId, EntityRepository entityRepository) =>
{
    try
    {
        var result = entityRepository.GetUpdateModel(entityId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/entity/list", (EntityRepository entityRepository) =>
{
    try
    {
        var result = entityRepository.GetAll(f => true);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapGet("/entity/list/withBaseFields", (EntityRepository entityRepository) =>
{
    try
    {
        var result = entityRepository.GetListBasic();
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapPost("/entity", (EntityCreateDto createDto, EntityRepository entityRepository) =>
{
    try
    {
        if (string.IsNullOrEmpty(createDto.Name) || string.IsNullOrEmpty(createDto.TableName) || createDto.Fields.Count == 0)
            return Results.BadRequest("Check The Fields!");

        entityRepository.Create(createDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPut("/entity", (EntityUpdateDto updateDto, EntityRepository entityRepository) =>
{
    try
    {
        if (string.IsNullOrEmpty(updateDto.Name) || string.IsNullOrEmpty(updateDto.TableName))
            return Results.BadRequest("Check The Fields!");

        entityRepository.Update(updateDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapDelete("/entity", (int id, EntityRepository entityRepository) =>
{
    try
    {
        entityRepository.Delete(id);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region FieldType
// ok
app.MapGet("/fieldType/list/onbasetype", (FieldTypeRepository fieldTypeRepository) =>
{
    try
    {
        var result = fieldTypeRepository.GetAll(filter: f => f.SourceTypeId == (byte)FieldTypeSourceEnums.Base);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region Field
app.MapGet("/field", (int id, FieldRepository fieldRepository) =>
{
    try
    {
        var field = fieldRepository.Get(f => f.Id == id);
        if (field is null)
            return Results.NotFound();
        return Results.Ok(field);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/field/list/byEntity", (int entityId, FieldRepository fieldRepository) =>
{
    try
    {
        var field = fieldRepository.GetAll(f => f.EntityId == entityId);
        if (field is null)
            return Results.NotFound();
        return Results.Ok(field);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapGet("/field/list/updateModel", (int entityId, FieldRepository fieldRepository) =>
{
    try
    {
        var field = fieldRepository.GetUpdateModels(entityId);
        if (field is null)
            return Results.NotFound();
        return Results.Ok(field);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPost("/field", (FieldCreateDto createDto, FieldRepository fieldRepository) =>
{
    try
    {
        if (string.IsNullOrEmpty(createDto.Name) || createDto.EntityId == default || createDto.FieldTypeId == default)
            return Results.BadRequest("Check The Fields!");

        fieldRepository.Add(createDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPut("/field", (FieldUpdateDto updateDto, FieldRepository fieldRepository) =>
{
    try
    {
        if (updateDto.Name == default || updateDto.FieldTypeId == default)
            return Results.BadRequest("Check The Fields!");

        fieldRepository.Update(updateDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

// ok
app.MapPut("/field/list", ([FromQuery] int entityId, [FromBody] List<FieldUpdateDto> updateDtos, FieldRepository fieldRepository) =>
{
    try
    {
        fieldRepository.Update(updateDtos, entityId);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapDelete("/field", (int id, FieldRepository fieldRepository) =>
{
    try
    {
        fieldRepository.DeleteByFilter(f => f.Id == id);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region RelationType
app.MapGet("/relationType/list", (RelationRepository relationRepository) =>
{
    try
    {
        var result = relationRepository.GetRelationTypes();
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion 

#region DeleteBehaviorTypes
app.MapGet("/deleteBehaviorType/list", (DeleteBehaviorTypeRepository deleteBehaviorTypeRepository) =>
{
    try
    {
        var result = deleteBehaviorTypeRepository.GetAll();
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion 

#region Relation
app.MapGet("/relation", (int id, RelationRepository relationRepository) =>
{
    try
    {
        var relation = relationRepository.Get(
            filter: f => f.Id == id,
            include: i => i.Include(x => x.PrimaryField).Include(x => x.ForeignField)
        );
        if (relation is null)
            return Results.NotFound();
        return Results.Ok(relation);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/relation/list/byEntity", (int entityId, RelationRepository relationRepository) =>
{
    try
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
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/relation/list/behindEntities", (int firstEntityId, int secondEntityId, RelationRepository relationRepository) =>
{
    try
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
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPost("/relation", (RelationCreateDto createDto, RelationRepository relationRepository) =>
{
    try
    {
        if (createDto.PrimaryFieldId == default || createDto.ForeignFieldId == default || createDto.RelationTypeId == default || createDto.DeleteBehaviorTypeId == default || string.IsNullOrEmpty(createDto.PrimaryEntityVirPropName) || string.IsNullOrEmpty(createDto.ForeignEntityVirPropName))
            return Results.BadRequest("Check The Fields!");
        relationRepository.AddRelation(createDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPut("/relation", (RelationUpdateDto updateDto, RelationRepository relationRepository) =>
{
    try
    {
        if (updateDto.PrimaryFieldId == default || updateDto.ForeignFieldId == default || updateDto.RelationTypeId == default || updateDto.DeleteBehaviorTypeId == default || string.IsNullOrEmpty(updateDto.PrimaryEntityVirPropName) || string.IsNullOrEmpty(updateDto.ForeignEntityVirPropName))
            return Results.BadRequest("Check The Fields!");
        relationRepository.UpdateRelation(updateDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapDelete("/relation", (int id, RelationRepository relationRepository) =>
{
    try
    {
        var relation = relationRepository.Get(f => f.Id == id);
        if (relation is null)
            return Results.NotFound();
        relationRepository.Delete(relation);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region CrudType
app.MapGet("/crudtype/list", (CrudTypeRepository crudTypeRepository) =>
{
    try
    {
        var result = crudTypeRepository.GetAll(enableTracking: false);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region Dto
app.MapGet("/dto", (int entityId, DtoRepository dtoRepository) =>
{
    try
    {
        var result = dtoRepository.Get(f => f.RelatedEntityId == entityId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/dto/updateModel", (int dtoId, DtoRepository dtoRepository) =>
{
    try
    {
        var result = dtoRepository.GetUpdateModel(dtoId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/dto/list/byEntity", (int entityId, DtoRepository dtoRepository) =>
{
    try
    {
        var result = dtoRepository.GetAll(filter: f => f.RelatedEntityId == entityId, include: i => i.Include(x => x.CrudType), enableTracking: false);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/dto/list/detail", (int entityId, DtoRepository dtoRepository) =>
{
    try
    {
        var result = dtoRepository.GetDetailList(f => f.RelatedEntityId == entityId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPost("/dto", (DtoCreateDto createDto, DtoRepository dtoRepository) =>
{
    try
    {
        if (string.IsNullOrEmpty(createDto.Name) || createDto.RelatedEntityId == default || createDto.CrudTypeId == default)
            return Results.BadRequest("Check The Fields!");

        dtoRepository.CreateByFields(createDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPut("/dto", (DtoUpdateDto updateDto, DtoRepository dtoRepository) =>
{
    try
    {
        if (string.IsNullOrEmpty(updateDto.Name) || updateDto.RelatedEntityId == default || updateDto.CrudTypeId == default || updateDto.Id == default)
            return Results.BadRequest("Check The Fields!");
        dtoRepository.Update(updateDto);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapDelete("/dto", (int id, DtoRepository dtoRepository) =>
{
    try
    {
        dtoRepository.Delete(id);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region DtoField
app.MapGet("/dtofield", (int id, DtoFieldRepository dtoFieldRepository) =>
{
    try
    {
        var field = dtoFieldRepository.Get(f => f.Id == id);
        if (field is null)
            return Results.NotFound();
        return Results.Ok(field);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/dtoFiedRelations", (int dtoFieldId, DtoFieldRepository dtoFieldRepository) =>
{
    try
    {
        var field = dtoFieldRepository.GetDtoFieldRelations(dtoFieldId);
        if (field is null)
            return Results.NotFound();
        return Results.Ok(field);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/dtofield/list/updateModel", (int dtoId, DtoFieldRepository dtoFieldRepository) =>
{
    try
    {
        var dtofields = dtoFieldRepository.GetUpdateDtos(dtoId);
        if (dtofields is null)
            return Results.NotFound();
        return Results.Ok(dtofields);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});


// ok
app.MapPut("/dtofield/list", ([FromQuery] int dtoId, [FromBody] List<DtoFieldUpdateDto> updateDtos, DtoFieldRepository dtoFieldRepository) =>
{
    try
    {
        dtoFieldRepository.Update(updateDtos, dtoId);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region ValidatorType
app.MapGet("/validatorType", (int validatorTypeId, ValidationRepository validationRepository) =>
{
    try
    {
        var result = validationRepository.GetValidatorType(validatorTypeId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapGet("/validatorType/list", (ValidationRepository validationRepository) =>
{
    try
    {
        var result = validationRepository.GetValidatorTypes();
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region ValidatorTypeParams
app.MapGet("/validatorTypeParam/list", (int validatorTypeId, ValidationRepository validationRepository) =>
{
    try
    {
        var result = validationRepository.GetValidatorTypeParams(validatorTypeId);
        if (result is null)
            return Results.NotFound();
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

#region Validation
app.MapGet("/validation/list/updateModel", (int dtoFieldId, ValidationRepository validationRepository) =>
{
    try
    {
        var result = validationRepository.GetUpdateDtos(dtoFieldId);
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});

app.MapPost("/validation/list", (List<ValidationUpdateDto> updateDtos, ValidationRepository validationRepository) =>
{
    try
    {
        if (
            updateDtos.Any(f => f.ValidatorTypeId == default) ||
            updateDtos.Any(f => f.DtoFieldId == default) ||
            updateDtos.DistinctBy(f => f.DtoFieldId).ToList().Count > 1 ||
            updateDtos.Any(f => f.ValidationParams != null && f.ValidationParams.Any(fi => string.IsNullOrEmpty(fi.Value)))
        )
            return Results.BadRequest("Check The Fields!");

        validationRepository.SetValidations(updateDtos);
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.InternalServerError();
    }
});
#endregion

app.Run();