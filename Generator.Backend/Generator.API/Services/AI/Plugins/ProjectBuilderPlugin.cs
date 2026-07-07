using System.ComponentModel;
using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Entity;
using Generator.Domain.Core.Dtos.Field;
using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Repository;
using Microsoft.SemanticKernel;

namespace Generator.API.Services.AI.Plugins;

public class ProjectBuilderPlugin
{
    private readonly EntityRepository _entityRepo;
    private readonly FieldRepository _fieldRepo;
    private readonly RelationRepository _relationRepo;
    private readonly ProjectContext _context;

    public ProjectBuilderPlugin(
        EntityRepository entityRepo, 
        FieldRepository fieldRepo, 
        RelationRepository relationRepo,
        ProjectContext context)
    {
        _entityRepo = entityRepo;
        _fieldRepo = fieldRepo;
        _relationRepo = relationRepo;
        _context = context;
    }

    [KernelFunction("CreateEntity")]
    [Description("Creates a new Entity (Database Table) in the architecture.")]
    public string CreateEntity(
        [Description("Name of the Entity in singular form (e.g., Product)")] string entityName,
        [Description("Does the entity support Soft Delete? (default: false)")] bool softDeletable = false,
        [Description("Is the entity Auditable? (default: false)")] bool auditable = false,
        [Description("Is the entity Archivable? (default: false)")] bool archivable = false)
    {
        try
        {
            if (_entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).Any())
                return $"Entity '{entityName}' already exists.";

            var dto = new EntityCreateDto
            {
                Name = entityName,
                TableName = entityName + "s",
                SoftDeletable = softDeletable,
                Auditable = auditable,
                Archivable = archivable,
                Fields = new List<FieldCreateDto>()
            };

            _entityRepo.Create(dto);
            return $"Entity '{entityName}' created successfully.";
        }
        catch (Exception ex)
        {
            return $"Error creating Entity: {ex.Message}";
        }
    }

    [KernelFunction("AddField")]
    [Description("Adds a new Field (Column) to an existing Entity.")]
    public string AddField(
        [Description("The name of the Entity to add the field to (e.g., Product)")] string entityName,
        [Description("The name of the Field (e.g., Price, Name, IsActive)")] string fieldName,
        [Description("The C# data type of the Field (e.g., string, int, decimal, bool, datetime, byte)")] string fieldType,
        [Description("Is this field mandatory?")] bool isRequired,
        [Description("Must this field's values be unique? (default: false)")] bool isUnique = false,
        [Description("Is this field a list/collection? (default: false)")] bool isList = false,
        [Description("Can this field be used for filtering? (default: true)")] bool filterable = true)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{entityName}' not found. Create the entity first.";

            var dbFieldType = _context.FieldTypes.FirstOrDefault(f => f.Name.ToLower() == fieldType.ToLower());
            if (dbFieldType == null) return $"Error: Data type '{fieldType}' is not supported.";

            var dto = new FieldCreateDto
            {
                EntityId = entity.Id,
                Name = fieldName,
                FieldTypeId = dbFieldType.Id,
                IsRequired = isRequired,
                IsUnique = isUnique,
                IsList = isList,
                Filterable = filterable
            };

            _fieldRepo.Add(dto);
            return $"Field '{fieldName}' ({fieldType}) successfully added to Entity '{entityName}'.";
        }
        catch (Exception ex)
        {
            return $"Error adding Field: {ex.Message}";
        }
    }

    [KernelFunction("CreateRelation")]
    [Description("Establishes a database relationship between two existing Entities.")]
    public string CreateRelation(
        [Description("The name of the Source (Primary) Entity (e.g., Category)")] string primaryEntityName,
        [Description("The name of the Target (Foreign) Entity (e.g., Product)")] string foreignEntityName,
        [Description("Relation Type ID (1=OneToOne, 2=OneToMany)")] int relationTypeId,
        [Description("Delete Behavior Type ID (1=Cascade, 3=Restrict, 6=SetNull, 7=NoAction; call GetSystemDictionaryTypes for the full list)")] int deleteBehaviorTypeId = 1)
    {
        try
        {
            var primaryEntity = _entityRepo.GetAll(e => e.Name.ToLower() == primaryEntityName.ToLower()).FirstOrDefault();
            var foreignEntity = _entityRepo.GetAll(e => e.Name.ToLower() == foreignEntityName.ToLower()).FirstOrDefault();

            if (primaryEntity == null) return $"Error: Primary Entity '{primaryEntityName}' not found.";
            if (foreignEntity == null) return $"Error: Foreign Entity '{foreignEntityName}' not found.";

            // Find primary key of primary entity (Id)
            var primaryField = _context.Fields.FirstOrDefault(f => f.EntityId == primaryEntity.Id && f.Name == "Id");
            if (primaryField == null) return "Error: 'Id' field not found in Primary Entity.";

            // Create Foreign Key field in the Foreign Entity
            var fkFieldName = primaryEntity.Name + "Id";
            var fkFieldType = _context.FieldTypes.FirstOrDefault(f => f.Name == "int"); // Usually int for Identity
            
            var addedFkField = _fieldRepo.Add(new FieldCreateDto
            {
                EntityId = foreignEntity.Id,
                Name = fkFieldName,
                FieldTypeId = fkFieldType?.Id ?? 1,
                IsRequired = true,
                IsUnique = false,
                IsList = false,
                Filterable = true
            });

            var dto = new RelationCreateDto
            {
                PrimaryFieldId = primaryField.Id,
                ForeignFieldId = addedFkField.Id,
                RelationTypeId = relationTypeId,
                DeleteBehaviorTypeId = deleteBehaviorTypeId,
                PrimaryEntityVirPropName = foreignEntity.Name + "s", // e.g., Products
                ForeignEntityVirPropName = primaryEntity.Name // e.g., Category
            };

            _relationRepo.AddRelation(dto);
            return $"Relation successfully created between '{primaryEntityName}' and '{foreignEntityName}'.";
        }
        catch (Exception ex)
        {
            return $"Error creating Relation: {ex.Message}";
        }
    }

    [KernelFunction("UpdateEntity")]
    [Description("Updates the name or configuration flags of an existing Entity. Inspect the entity first (GetEntitiesAndFields) and pass its current flag values for the flags you don't want to change.")]
    public string UpdateEntity(
        [Description("The current name of the Entity (e.g., Urunler)")] string oldEntityName,
        [Description("The new name of the Entity (pass the old name if you don't want to change it)")] string newEntityName,
        [Description("Soft Delete support (pass the current value to keep it)")] bool softDeletable,
        [Description("Auditable support (pass the current value to keep it)")] bool auditable,
        [Description("Archivable support (pass the current value to keep it)")] bool archivable)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == oldEntityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{oldEntityName}' not found.";

            var updateDto = _entityRepo.GetUpdateModel(entity.Id);
            updateDto.Name = newEntityName;
            updateDto.TableName = newEntityName + "s";

            updateDto.SoftDeletable = softDeletable;
            updateDto.Auditable = auditable;
            updateDto.Archivable = archivable;

            _entityRepo.Update(updateDto);
            return $"Entity '{oldEntityName}' successfully renamed to '{newEntityName}'.";
        }
        catch (Exception ex)
        {
            return $"Error updating Entity: {ex.Message}";
        }
    }

    [KernelFunction("DeleteEntity")]
    [Description("Permanently deletes an Entity and cascades to delete all its fields and relationships. Use with caution.")]
    public string DeleteEntity([Description("Name of the Entity to delete")] string entityName)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{entityName}' not found.";

            _entityRepo.Delete(entity.Id);
            return $"Entity '{entityName}' and all its contents have been permanently deleted.";
        }
        catch (Exception ex)
        {
            return $"Error deleting Entity: {ex.Message}";
        }
    }

    [KernelFunction("UpdateField")]
    [Description("Updates the properties (name, type, requirements) of an existing Field. Inspect the field first (GetEntitiesAndFields) and pass its current values for the flags you don't want to change.")]
    public string UpdateField(
        [Description("The name of the Entity the field belongs to")] string entityName,
        [Description("The current name of the Field")] string oldFieldName,
        [Description("The new name of the Field")] string newFieldName,
        [Description("The new C# data type (string, int, decimal, bool, datetime)")] string newFieldType,
        [Description("Is this field mandatory?")] bool isRequired,
        [Description("Must values be unique? (pass the current value to keep it)")] bool isUnique,
        [Description("Is it a list/collection? (pass the current value to keep it)")] bool isList,
        [Description("Can it be used for filtering? (pass the current value to keep it)")] bool filterable)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{entityName}' not found.";

            var field = _fieldRepo.GetAll(f => f.EntityId == entity.Id && f.Name.ToLower() == oldFieldName.ToLower()).FirstOrDefault();
            if (field == null) return $"Error: Field '{oldFieldName}' not found in Entity '{entityName}'.";

            var dbFieldType = _context.FieldTypes.FirstOrDefault(f => f.Name.ToLower() == newFieldType.ToLower());
            if (dbFieldType == null) return $"Error: Data type '{newFieldType}' is not supported.";

            var updateDto = new Generator.Domain.Core.Dtos.Field.FieldUpdateDto
            {
                Id = field.Id,
                FieldTypeId = dbFieldType.Id,
                Name = newFieldName,
                IsRequired = isRequired,
                IsUnique = isUnique,
                IsList = isList,
                Filterable = filterable
            };

            _fieldRepo.Update(updateDto);
            return $"Field '{oldFieldName}' in '{entityName}' successfully updated to '{newFieldName}' ({newFieldType}).";
        }
        catch (Exception ex)
        {
            return $"Error updating Field: {ex.Message}";
        }
    }

    [KernelFunction("DeleteField")]
    [Description("Permanently deletes a Field from an Entity.")]
    public string DeleteField(
        [Description("The name of the Entity the field belongs to")] string entityName,
        [Description("The name of the Field to delete")] string fieldName)
    {
        try
        {
            var entity = _entityRepo.GetAll(e => e.Name.ToLower() == entityName.ToLower()).FirstOrDefault();
            if (entity == null) return $"Error: Entity '{entityName}' not found.";

            var field = _fieldRepo.GetAll(f => f.EntityId == entity.Id && f.Name.ToLower() == fieldName.ToLower()).FirstOrDefault();
            if (field == null) return $"Error: Field '{fieldName}' not found in Entity '{entityName}'.";

            _context.Fields.Remove(field);
            _context.SaveChanges();
            return $"Field '{fieldName}' permanently deleted from '{entityName}'.";
        }
        catch (Exception ex)
        {
            return $"Error deleting Field: {ex.Message}";
        }
    }

    [KernelFunction("DeleteRelation")]
    [Description("Permanently deletes an established database relationship between two Entities.")]
    public string DeleteRelation(
        [Description("The name of the Primary (Source) Entity")] string primaryEntityName,
        [Description("The name of the Foreign (Target) Entity")] string foreignEntityName)
    {
        try
        {
            var primaryEntity = _entityRepo.GetAll(e => e.Name.ToLower() == primaryEntityName.ToLower()).FirstOrDefault();
            var foreignEntity = _entityRepo.GetAll(e => e.Name.ToLower() == foreignEntityName.ToLower()).FirstOrDefault();

            if (primaryEntity == null) return $"Error: Primary Entity '{primaryEntityName}' not found.";
            if (foreignEntity == null) return $"Error: Foreign Entity '{foreignEntityName}' not found.";

            // Bulunan relation'ı sil
            var relation = _context.Relations.FirstOrDefault(r => 
                r.PrimaryField.EntityId == primaryEntity.Id && 
                r.ForeignField.EntityId == foreignEntity.Id);

            if (relation == null) return $"Error: No relation found between '{primaryEntityName}' and '{foreignEntityName}'.";

            _context.Relations.Remove(relation);
            _context.SaveChanges();
            return $"Relation between '{primaryEntityName}' and '{foreignEntityName}' permanently deleted.";
        }
        catch (Exception ex)
        {
            return $"Error deleting Relation: {ex.Message}";
        }
    }

    [KernelFunction("UpdateRelation")]
    [Description("Updates an existing database relationship between two Entities (e.g. changing Delete Behavior). Inspect the relation first (GetRelations) and pass its current values for the ones you don't want to change.")]
    public string UpdateRelation(
        [Description("The name of the Primary (Source) Entity")] string primaryEntityName,
        [Description("The name of the Foreign (Target) Entity")] string foreignEntityName,
        [Description("Relation Type ID (1=OneToOne, 2=OneToMany; pass the current value to keep it)")] int relationTypeId,
        [Description("Delete Behavior Type ID (1=Cascade, 3=Restrict, 6=SetNull, 7=NoAction; pass the current value to keep it)")] int deleteBehaviorTypeId)
    {
        try
        {
            var primaryEntity = _entityRepo.GetAll(e => e.Name.ToLower() == primaryEntityName.ToLower()).FirstOrDefault();
            var foreignEntity = _entityRepo.GetAll(e => e.Name.ToLower() == foreignEntityName.ToLower()).FirstOrDefault();

            if (primaryEntity == null) return $"Error: Primary Entity '{primaryEntityName}' not found.";
            if (foreignEntity == null) return $"Error: Foreign Entity '{foreignEntityName}' not found.";

            var relation = _context.Relations.FirstOrDefault(r => 
                r.PrimaryField.EntityId == primaryEntity.Id && 
                r.ForeignField.EntityId == foreignEntity.Id);

            if (relation == null) return $"Error: No relation found between '{primaryEntityName}' and '{foreignEntityName}'.";

            var updateDto = new RelationUpdateDto
            {
                Id = relation.Id,
                PrimaryFieldId = relation.PrimaryFieldId,
                ForeignFieldId = relation.ForeignFieldId,
                RelationTypeId = relationTypeId,
                DeleteBehaviorTypeId = deleteBehaviorTypeId,
                PrimaryEntityVirPropName = relation.PrimaryEntityVirPropName,
                ForeignEntityVirPropName = relation.ForeignEntityVirPropName
            };

            _relationRepo.UpdateRelation(updateDto);
            return $"Relation between '{primaryEntityName}' and '{foreignEntityName}' successfully updated.";
        }
        catch (Exception ex)
        {
            return $"Error updating Relation: {ex.Message}";
        }
    }

    [InspectorFunction]
    [KernelFunction("GetEntitiesAndFields")]
    [Description("Retrieves the current database schema, including all existing Entities and their Fields. Use this to inspect the architecture before making modifications.")]
    public string GetEntitiesAndFields()
    {
        var entities = _context.Entities.ToList();
        var fields = _context.Fields.ToList();

        if (!entities.Any()) return "There are no entities in the database yet.";

        var sb = new System.Text.StringBuilder();
        foreach (var entity in entities)
        {
            var entityFields = fields.Where(f => f.EntityId == entity.Id).Select(f =>
                $"{f.Name} ({f.GetMapedTypeName()}, Required={f.IsRequired}, Unique={f.IsUnique}, Filterable={f.Filterable})");
            sb.AppendLine($"- Entity: {entity.Name} (SoftDeletable={entity.SoftDeletable}, Auditable={entity.Auditable}, Archivable={entity.Archivable}) | Fields: {string.Join(", ", entityFields)}");
        }
        return sb.ToString();
    }

    [InspectorFunction]
    [KernelFunction("GetRelations")]
    [Description("Retrieves all existing database relationships between entities. Use this to check existing foreign keys.")]
    public string GetRelations()
    {
        var relations = _context.Relations.ToList();
        var fields = _context.Fields.ToList();
        var entities = _context.Entities.ToList();
        var relationTypes = _context.RelationTypes.ToList();
        var deleteBehaviorTypes = _context.DeleteBehaviorTypes.ToList();

        if (!relations.Any()) return "There are no relationships in the database yet.";

        var sb = new System.Text.StringBuilder();
        foreach (var rel in relations)
        {
            var primaryField = fields.FirstOrDefault(f => f.Id == rel.PrimaryFieldId);
            var foreignField = fields.FirstOrDefault(f => f.Id == rel.ForeignFieldId);
            if (primaryField != null && foreignField != null)
            {
                var primaryEntity = entities.FirstOrDefault(e => e.Id == primaryField.EntityId)?.Name;
                var foreignEntity = entities.FirstOrDefault(e => e.Id == foreignField.EntityId)?.Name;
                var relType = relationTypes.FirstOrDefault(r => r.Id == rel.RelationTypeId);
                var delBehavior = deleteBehaviorTypes.FirstOrDefault(d => d.Id == rel.DeleteBehaviorTypeId);
                sb.AppendLine($"- Type: {relType?.Name ?? rel.RelationTypeId.ToString()} | DeleteBehavior: {delBehavior?.Name ?? "Unknown"} | Between: {primaryEntity} and {foreignEntity}");
            }
        }
        return sb.ToString();
    }
}
