using Generator.Domain.Context;
using Generator.Domain.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Repository.Base;

namespace Generator.Domain.Repository;

public class RelationRepository : EFRepositoryBase<Relation>
{
    public RelationRepository(ProjectContext context) : base(context)
    {
    }

    public List<Relation> GetRelationsOnPrimary(int entityId)
    {
        var fieldsOfEntities = _context.Fields.Where(f => f.EntityId == entityId).AsNoTracking().Select(d => d.Id).ToList();

        var query = _context.Relations
              .Include(i => i.ForeignField)
                  .ThenInclude(ti => ti.Entity)
              .Include(i => i.PrimaryField)
                  .ThenInclude(ti => ti.Entity)
              .AsNoTracking().ToList();
        return query.Where(f => fieldsOfEntities.Any(s => s == f.PrimaryFieldId)).ToList();
    }

    public List<Relation> GetRelationsOnForeign(int entityId)
    {
        var fieldsOfEntities = _context.Fields.Where(f => f.EntityId == entityId).AsNoTracking().Select(d => d.Id).ToList();

        var query = _context.Relations
             .Include(i => i.ForeignField)
                 .ThenInclude(ti => ti.Entity)
             .Include(i => i.PrimaryField)
                 .ThenInclude(ti => ti.Entity)
             .AsNoTracking().ToList();
        return query.Where(f => fieldsOfEntities.Any(s => s == f.ForeignFieldId)).ToList();
    }

    public List<Relation> GetRelationsBehindEntities(int entityId_ofPrimaryField, int entityId_ofForeignField)
    {
        return _context.Relations
          .Include(r => r.PrimaryField)
              .ThenInclude(f => f.Entity)
          .Include(r => r.ForeignField)
              .ThenInclude(f => f.Entity)
          .Where(r =>
                (
                    (r.ForeignField.Entity.Id == entityId_ofForeignField && r.PrimaryField.Entity.Id == entityId_ofPrimaryField) ||
                    (r.ForeignField.Entity.Id == entityId_ofPrimaryField && r.PrimaryField.Entity.Id == entityId_ofForeignField)
                )
          ).ToList();
    }

    public void AddRelation(RelationCreateDto createDto)
    {
        _context.Relations.Add(new Relation
        {
            PrimaryFieldId = createDto.PrimaryFieldId,
            ForeignFieldId = createDto.ForeignFieldId,
            RelationTypeId = createDto.RelationTypeId,
            DeleteBehaviorTypeId = createDto.DeleteBehaviorTypeId,
            PrimaryEntityVirPropName = createDto.PrimaryEntityVirPropName!,
            ForeignEntityVirPropName = createDto.ForeignEntityVirPropName!
        });
        _context.SaveChanges();
    }

    public void UpdateRelation(RelationUpdateDto updateDto)
    {
        var relation = _context.Relations.FirstOrDefault(f => f.Id == updateDto.Id);
        if (relation == null) throw new Exception("Relation not found to update.");

        relation.PrimaryFieldId = updateDto.PrimaryFieldId;
        relation.ForeignFieldId = updateDto.ForeignFieldId;
        relation.RelationTypeId = updateDto.RelationTypeId;
        relation.DeleteBehaviorTypeId = updateDto.DeleteBehaviorTypeId;
        relation.PrimaryEntityVirPropName = updateDto.PrimaryEntityVirPropName!;
        relation.ForeignEntityVirPropName = updateDto.ForeignEntityVirPropName!;

        _context.Update(relation);
        _context.SaveChanges();
    }

    public List<RelationType> GetRelationTypes()
    {
        return _context.RelationTypes.ToList();
    }
}
