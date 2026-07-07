using System.ComponentModel;
using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Dto;
using Generator.Domain.Core.Dtos.DtoField;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Repository;
using Microsoft.SemanticKernel;

namespace Generator.API.Services.AI.Plugins;

public class DtoPlugin
{
    private readonly DtoRepository _dtoRepo;
    private readonly DtoFieldRepository _dtoFieldRepo;
    private readonly EntityRepository _entityRepo;
    private readonly ProjectContext _context;

    public DtoPlugin(DtoRepository dtoRepo, DtoFieldRepository dtoFieldRepo, EntityRepository entityRepo, ProjectContext context)
    {
        _dtoRepo = dtoRepo;
        _dtoFieldRepo = dtoFieldRepo;
        _entityRepo = entityRepo;
        _context = context;
    }

    // ===========================
    // DTO-Level CRUD
    // ===========================

    [KernelFunction("CreateDto")]
    [Description("Creates a new DTO (Data Transfer Object) for a specified Entity. E.g., CreateProductDto for Product.")]
    public string CreateDto(
        [Description("Name of the target Entity (e.g., Product)")] string entityName,
        [Description("Full name of the DTO to be created (e.g., CreateProductDto)")] string dtoName,
        [Description("CRUD Type ID indicating the purpose (1=Read, 2=Create, 3=Update, 4=Delete)")] byte crudTypeId)
    {
        try
        {
            var entity = _context.Entities.FirstOrDefault(e => e.Name.ToLower() == entityName.ToLower());
            if (entity == null) return $"Error: Entity '{entityName}' not found.";

            var dto = new DtoCreateDto
            {
                Name = dtoName,
                RelatedEntityId = entity.Id,
                CrudTypeId = crudTypeId,
                DtoFields = new List<DtoFieldCreateDto>()
            };

            _dtoRepo.CreateByFields(dto);
            return $"DTO '{dtoName}' created successfully for Entity '{entityName}'.";
        }
        catch (Exception ex)
        {
            return $"Error creating DTO: {ex.Message}";
        }
    }

    [KernelFunction("UpdateDto")]
    [Description("Updates an existing DTO's name or CRUD type. Inspect the DTO first (GetDtos) and pass its current CRUD type if you don't want to change it.")]
    public string UpdateDto(
        [Description("The current name of the DTO")] string oldDtoName,
        [Description("The new name of the DTO")] string newDtoName,
        [Description("CRUD Type ID (1=Read, 2=Create, 3=Update, 4=Delete; pass the current value to keep it)")] byte crudTypeId)
    {
        try
        {
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == oldDtoName.ToLower());
            if (dto == null) return $"Error: DTO '{oldDtoName}' not found.";

            dto.Name = newDtoName;
            dto.CrudTypeId = crudTypeId;

            _context.Dtos.Update(dto);
            _context.SaveChanges();
            return $"DTO '{oldDtoName}' successfully updated to '{newDtoName}'.";
        }
        catch (Exception ex)
        {
            return $"Error updating DTO: {ex.Message}";
        }
    }

    [KernelFunction("DeleteDto")]
    [Description("Permanently deletes an existing DTO from the database.")]
    public string DeleteDto([Description("The name of the DTO to delete (e.g., CreateProductDto)")] string dtoName)
    {
        try
        {
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            _dtoRepo.Delete(dto.Id);
            return $"DTO '{dtoName}' successfully deleted.";
        }
        catch (Exception ex)
        {
            return $"Error deleting DTO: {ex.Message}";
        }
    }

    // ===========================
    // DTO Field-Level CRUD
    // ===========================

    [KernelFunction("AddDtoField")]
    [Description("Adds a field to an existing DTO. The field can come from the DTO's own Entity or from a related Entity (cross-entity field). For cross-entity fields, the system automatically detects the relation chain.")]
    public string AddDtoField(
        [Description("Name of the DTO to add the field to (e.g., ProductDetailDto)")] string dtoName,
        [Description("Name of the Entity that owns the source field (e.g., Stock for a stock field, Product for a product field)")] string sourceEntityName,
        [Description("Name of the source field in that Entity (e.g., Quantity, Price)")] string sourceFieldName,
        [Description("Display name of the field in the DTO (e.g., StockQuantity). If empty, uses sourceFieldName.")] string fieldName = "",
        [Description("Is this field required in the DTO?")] bool isRequired = false,
        [Description("Is this field a list/collection?")] bool isList = false)
    {
        try
        {
            // 1. Find the DTO
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            // 2. Find the source entity
            var sourceEntity = _context.Entities.FirstOrDefault(e => e.Name.ToLower() == sourceEntityName.ToLower());
            if (sourceEntity == null) return $"Error: Source Entity '{sourceEntityName}' not found.";

            // 3. Find the source field
            var sourceField = _context.Fields.FirstOrDefault(f => f.EntityId == sourceEntity.Id && f.Name.ToLower() == sourceFieldName.ToLower());
            if (sourceField == null) return $"Error: Field '{sourceFieldName}' not found in Entity '{sourceEntityName}'.";

            // 4. Check for duplicate
            var existingField = _context.DtoFields.FirstOrDefault(f => f.DtoId == dto.Id && f.SourceFieldId == sourceField.Id);
            if (existingField != null) return $"Error: Field '{sourceFieldName}' already exists in DTO '{dtoName}'.";

            // 5. Determine if this is a cross-entity field (source entity != DTO's related entity)
            var displayName = string.IsNullOrWhiteSpace(fieldName) ? sourceFieldName : fieldName;
            List<DtoFieldRelationsCreateModel>? dtoFieldRelations = null;

            if (sourceEntity.Id != dto.RelatedEntityId)
            {
                // Cross-entity field: auto-detect the relation chain
                dtoFieldRelations = BuildRelationChain(dto.RelatedEntityId, sourceEntity.Id);
                if (dtoFieldRelations == null || !dtoFieldRelations.Any())
                    return $"Error: No relation path found between DTO's Entity (ID={dto.RelatedEntityId}) and '{sourceEntityName}'. Create a relation first.";
            }

            // 6. Create the DtoField
            var createDto = new DtoFieldCreateDto
            {
                DtoId = dto.Id,
                SourceEntityId = sourceEntity.Id,
                SourceFieldId = sourceField.Id,
                Name = displayName,
                IsRequired = isRequired,
                IsList = isList,
                DtoFieldRelations = dtoFieldRelations
            };

            _dtoFieldRepo.Add(createDto);
            var relInfo = dtoFieldRelations != null ? " (cross-entity, relation chain auto-detected)" : "";
            return $"Field '{displayName}' (from {sourceEntityName}.{sourceFieldName}) successfully added to DTO '{dtoName}'{relInfo}.";
        }
        catch (Exception ex)
        {
            return $"Error adding DtoField: {ex.Message}";
        }
    }

    [KernelFunction("RemoveDtoField")]
    [Description("Removes a specific field from an existing DTO.")]
    public string RemoveDtoField(
        [Description("Name of the DTO (e.g., ProductDetailDto)")] string dtoName,
        [Description("Name of the field to remove from the DTO")] string fieldName)
    {
        try
        {
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            var dtoField = _context.DtoFields.FirstOrDefault(f => f.DtoId == dto.Id && f.Name.ToLower() == fieldName.ToLower());
            if (dtoField == null) return $"Error: Field '{fieldName}' not found in DTO '{dtoName}'.";

            // Remove associated DtoFieldRelations first
            var relations = _context.DtoFieldRelations.Where(r => r.DtoFieldId == dtoField.Id);
            if (relations.Any())
            {
                _context.DtoFieldRelations.RemoveRange(relations);
            }

            _context.DtoFields.Remove(dtoField);
            _context.SaveChanges();
            return $"Field '{fieldName}' successfully removed from DTO '{dtoName}'.";
        }
        catch (Exception ex)
        {
            return $"Error removing DtoField: {ex.Message}";
        }
    }

    // ===========================
    // Entity-DTO Binding
    // ===========================

    [KernelFunction("BindDtoToEntity")]
    [Description("Binds a DTO to an Entity as its designated Create, Update, Delete, Report, BasicResponse, or DetailResponse DTO. This is required for code generation to know which DTO serves which purpose.")]
    public string BindDtoToEntity(
        [Description("Name of the Entity (e.g., Product)")] string entityName,
        [Description("Name of the DTO to bind (e.g., CreateProductDto)")] string dtoName,
        [Description("Binding purpose: 'Create', 'Update', 'Delete', 'Report', 'BasicResponse', or 'DetailResponse'")] string bindingType)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{entityName}' not found.";

            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            var updateModel = _entityRepo.GetUpdateModel(entity.Id);

            switch (bindingType.ToLower().Trim())
            {
                case "create":
                    updateModel.CreateDtoId = dto.Id;
                    break;
                case "update":
                    updateModel.UpdateDtoId = dto.Id;
                    break;
                case "delete":
                    updateModel.DeleteDtoId = dto.Id;
                    break;
                case "report":
                    updateModel.ReportDtoId = dto.Id;
                    break;
                case "basicresponse":
                    updateModel.BasicResponseDtoId = dto.Id;
                    break;
                case "detailresponse":
                    updateModel.DetailResponseDtoId = dto.Id;
                    break;
                default:
                    return $"Error: Invalid binding type '{bindingType}'. Valid values: Create, Update, Delete, Report, BasicResponse, DetailResponse.";
            }

            _entityRepo.Update(updateModel);
            return $"DTO '{dtoName}' successfully bound to Entity '{entityName}' as '{bindingType}' DTO.";
        }
        catch (Exception ex)
        {
            return $"Error binding DTO to Entity: {ex.Message}";
        }
    }

    // ===========================
    // Inspector
    // ===========================

    [InspectorFunction]
    [KernelFunction("GetDtos")]
    [Description("Retrieves the full list of existing DTOs, including their specific fields and relational constraints. Use this to check which DTOs have already been created and what columns they hold.")]
    public string GetDtos()
    {
        var dtos = _context.Dtos.ToList();
        if (!dtos.Any()) return "There are no DTOs in the database yet.";

        var dtoFields = _context.DtoFields.ToList();
        var dtoFieldRelations = _context.DtoFieldRelations.ToList();
        var relations = _context.Relations.ToList();
        var entities = _context.Entities.ToList();
        var fields = _context.Fields.ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Existing DTOs Details:");
        foreach (var dto in dtos)
        {
            var relatedEntity = entities.FirstOrDefault(e => e.Id == dto.RelatedEntityId);
            var myFields = dtoFields.Where(f => f.DtoId == dto.Id).ToList();
            sb.AppendLine($"- DTO: {dto.Name} (Entity: {relatedEntity?.Name ?? "Unknown"}, CrudType: {dto.CrudTypeId})");
            foreach (var df in myFields)
            {
                var sourceField = fields.FirstOrDefault(f => f.Id == df.SourceFieldId);
                var sourceEntity = sourceField != null ? entities.FirstOrDefault(e => e.Id == sourceField.EntityId) : null;
                var typeStr = sourceField != null ? sourceField.GetMapedTypeName() : "unknown";
                var rels = dtoFieldRelations.Where(r => r.DtoFieldId == df.Id).ToList();
                
                if (rels.Any())
                {
                    var relNames = rels.Select(r => {
                        var relation = relations.FirstOrDefault(x => x.Id == r.RelationId);
                        if (relation == null) return "UnknownRelation";
                        var primaryF = fields.FirstOrDefault(f => f.Id == relation.PrimaryFieldId);
                        var foreignF = fields.FirstOrDefault(f => f.Id == relation.ForeignFieldId);
                        var primaryE = primaryF != null ? entities.FirstOrDefault(e => e.Id == primaryF.EntityId)?.Name : "P";
                        var foreignE = foreignF != null ? entities.FirstOrDefault(e => e.Id == foreignF.EntityId)?.Name : "F";
                        return $"{primaryE}->{foreignE}";
                    });
                    sb.AppendLine($"  * Field: {df.Name} ({typeStr}, from: {sourceEntity?.Name ?? "Unknown"}) [Relations: {string.Join(", ", relNames)}]");
                }
                else
                {
                    sb.AppendLine($"  * Field: {df.Name} ({typeStr})");
                }
            }
        }

        return sb.ToString();
    }

    // ===========================
    // Private Helpers
    // ===========================

    /// <summary>
    /// Auto-detects the direct relation chain between two entities.
    /// Handles single-hop relations (e.g., Product -> Stock).
    /// </summary>
    private List<DtoFieldRelationsCreateModel>? BuildRelationChain(int fromEntityId, int toEntityId)
    {
        var allRelations = _context.Relations.ToList();
        var allFields = _context.Fields.ToList();

        // Try to find a direct relation (single hop)
        foreach (var rel in allRelations)
        {
            var primaryField = allFields.FirstOrDefault(f => f.Id == rel.PrimaryFieldId);
            var foreignField = allFields.FirstOrDefault(f => f.Id == rel.ForeignFieldId);
            if (primaryField == null || foreignField == null) continue;

            // Check both directions
            if ((primaryField.EntityId == fromEntityId && foreignField.EntityId == toEntityId) ||
                (foreignField.EntityId == fromEntityId && primaryField.EntityId == toEntityId))
            {
                return new List<DtoFieldRelationsCreateModel>
                {
                    new DtoFieldRelationsCreateModel
                    {
                        FirstEntityId = fromEntityId,
                        SecondEntityId = toEntityId,
                        RelationId = rel.Id,
                        SequenceNo = 1
                    }
                };
            }
        }

        // No direct relation found
        return null;
    }
}
