using Generator.Domain.Context;
using Generator.Domain.Services;

namespace Generator.API.Services.AI;

/// <summary>
/// Builds a compact, token-friendly summary of the active project's current metadata
/// (entities, relations, DTOs). Injected into the chat as a system message so the model
/// knows the project state up front instead of spending an LLM round-trip on inspector tools.
/// </summary>
public class ProjectSnapshotService
{
    private readonly ProjectContext _context;
    private readonly IActiveProjectStore _activeProjectStore;

    public ProjectSnapshotService(ProjectContext context, IActiveProjectStore activeProjectStore)
    {
        _context = context;
        _activeProjectStore = activeProjectStore;
    }

    /// <summary>Returns the snapshot text, or null when no project is active or its database is unreachable.</summary>
    public string? BuildSnapshot()
    {
        var project = _activeProjectStore.ActiveProject;
        if (project == null) return null;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CURRENT PROJECT STATE (summary snapshot; call inspector tools for exact, up-to-date details):");
            sb.AppendLine($"Active Project: {project.ProjectName}");

            var entities = _context.Entities.ToList();
            var fields = _context.Fields.ToList();
            if (entities.Count == 0)
            {
                sb.AppendLine("Entities: none yet.");
            }
            else
            {
                sb.AppendLine("Entities:");
                foreach (var entity in entities)
                {
                    var flags = new List<string>();
                    if (entity.SoftDeletable) flags.Add("SoftDeletable");
                    if (entity.Auditable) flags.Add("Auditable");
                    if (entity.Archivable) flags.Add("Archivable");
                    var flagInfo = flags.Count > 0 ? $" [{string.Join(",", flags)}]" : "";

                    var entityFields = fields
                        .Where(f => f.EntityId == entity.Id)
                        .Select(f => $"{f.Name}:{f.GetMapedTypeName()}");
                    sb.AppendLine($"- {entity.Name}{flagInfo}: {string.Join(", ", entityFields)}");
                }
            }

            var relations = _context.Relations.ToList();
            if (relations.Count > 0)
            {
                var relationTypes = _context.RelationTypes.ToList();
                var deleteBehaviors = _context.DeleteBehaviorTypes.ToList();
                sb.AppendLine("Relations:");
                foreach (var rel in relations)
                {
                    var primaryField = fields.FirstOrDefault(f => f.Id == rel.PrimaryFieldId);
                    var foreignField = fields.FirstOrDefault(f => f.Id == rel.ForeignFieldId);
                    var primaryEntity = entities.FirstOrDefault(e => e.Id == primaryField?.EntityId)?.Name ?? "?";
                    var foreignEntity = entities.FirstOrDefault(e => e.Id == foreignField?.EntityId)?.Name ?? "?";
                    var relType = relationTypes.FirstOrDefault(t => t.Id == rel.RelationTypeId)?.Name ?? rel.RelationTypeId.ToString();
                    var behavior = deleteBehaviors.FirstOrDefault(d => d.Id == rel.DeleteBehaviorTypeId)?.Name ?? rel.DeleteBehaviorTypeId.ToString();
                    sb.AppendLine($"- {primaryEntity} -> {foreignEntity} ({relType}, DeleteBehavior: {behavior})");
                }
            }

            var dtos = _context.Dtos.ToList();
            if (dtos.Count > 0)
            {
                var crudTypes = _context.CrudTypes.ToList();
                sb.AppendLine("DTOs:");
                foreach (var dto in dtos)
                {
                    var relatedEntity = entities.FirstOrDefault(e => e.Id == dto.RelatedEntityId)?.Name ?? "?";
                    var crud = crudTypes.FirstOrDefault(c => c.Id == dto.CrudTypeId)?.Name ?? dto.CrudTypeId.ToString();
                    sb.AppendLine($"- {dto.Name} (Entity: {relatedEntity}, CrudType: {crud})");
                }
            }

            return sb.ToString();
        }
        catch
        {
            // Snapshot is best-effort; without it the model falls back to inspector tools.
            return null;
        }
    }
}
