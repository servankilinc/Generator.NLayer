using Generator.Domain.Context;
using Generator.Domain.Core.Entities;
using Generator.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Core.Dtos.Relation;

namespace Generator.Domain.Repository;

public class RelationRepository : EFRepositoryBase<Relation>
{

    public List<Relation> GetRelationsOfEntity(int entityId)
    {
        using var _context = new ProjectContext();
        var fieldsOfEntities = _context.Fields.Where(f => f.EntityId == entityId).AsNoTracking().Select(d => d.Id).ToList();
        //return _context.Relations
        //        .Where(f =>
        //            fieldsOfEntities.Contains(f.PrimaryFieldId) || fieldsOfEntities.Contains(f.ForeignFieldId))
        //        .Include(i => i.ForeignField)
        //            .ThenInclude(ti => ti.Entity)
        //        .Include(i => i.PrimaryField)
        //            .ThenInclude(ti => ti.Entity)
        //        .AsNoTracking()
        //        .ToList(); // efcheck

        var query = _context.Relations
               .Include(i => i.ForeignField)
                   .ThenInclude(ti => ti.Entity)
               .Include(i => i.PrimaryField)
                   .ThenInclude(ti => ti.Entity)
               .AsNoTracking().ToList();
        return query.Where(f => fieldsOfEntities.Any(s => s == f.PrimaryFieldId) || fieldsOfEntities.Any(s => s == f.ForeignFieldId)).ToList();
    }

    public List<Relation> GetRelationsOnPrimary(int entityId)
    {
        using var _context = new ProjectContext();
        var fieldsOfEntities = _context.Fields.Where(f => f.EntityId == entityId).AsNoTracking().Select(d => d.Id).ToList();
        //return _context.Relations
        //        .Where(f => fieldsOfEntities.Contains(f.PrimaryFieldId))
        //        .Include(i => i.ForeignField)
        //            .ThenInclude(ti => ti.Entity)
        //        .Include(i => i.PrimaryField)
        //            .ThenInclude(ti => ti.Entity)
        //        .AsNoTracking()
        //        .ToList(); // efcheck

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
        using var _context = new ProjectContext();
        var fieldsOfEntities = _context.Fields.Where(f => f.EntityId == entityId).AsNoTracking().Select(d => d.Id).ToList();
        //return _context.Relations
        //        .Where(f => fieldsOfEntities.Contains(f.ForeignFieldId))
        //        .Include(i => i.ForeignField)
        //            .ThenInclude(ti => ti.Entity)
        //        .Include(i => i.PrimaryField)
        //            .ThenInclude(ti => ti.Entity)
        //        .AsNoTracking()
        //        .ToList(); // efcheck

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
        using var _context = new ProjectContext();
        // category Id primary 
        // blog CategoryId foreign
        return _context.Relations
          .Include(r => r.PrimaryField)
              .ThenInclude(f => f.Entity)
          .Include(r => r.ForeignField)
              .ThenInclude(f => f.Entity)
          .Where(r =>
                //r.RelationTypeId == (int)RelationTypeEnums.OneToMany ? 
                //    (r.ForeignField.Entity.Id == entityId_ofForeignField && r.PrimaryField.Entity.Id == entityId_ofPrimaryField) :
                (
                    (r.ForeignField.Entity.Id == entityId_ofForeignField && r.PrimaryField.Entity.Id == entityId_ofPrimaryField) ||
                    (r.ForeignField.Entity.Id == entityId_ofPrimaryField && r.PrimaryField.Entity.Id == entityId_ofForeignField)
                )
          ).ToList();
    }

    public void AddRelation(RelationCreateDto createDto)
    {
        using var _context = new ProjectContext();
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
        using var _context = new ProjectContext();
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
        using var _context = new ProjectContext();
        return _context.RelationTypes.ToList();
    }

    //public List<RelationResponseDto> GetAllByFields(int fieldId) 
    //{
    //    using var _context = new LocalContext();

    //    var res = from relation in _localContext.Relations.Where(f => f.PrimaryFieldId == fieldId || f.ForeignFieldId == fieldId)
    //              join pf in _localContext.Fields on relation.PrimaryFieldId equals pf.Id
    //              join ft in _localContext.FieldTypes on pf.FieldTypeId equals ft.Id
    //              join ff in _localContext.Fields on relation.ForeignFieldId equals ff.Id
    //              join ft2 in _localContext.FieldTypes on ff.FieldTypeId equals ft2.Id
    //              select new RelationResponseDto
    //              {
    //                  Id = relation.Id,
    //                  RelationTypeId = relation.RelationTypeId,
    //                  RelationType = new RelationTypeResponseDto
    //                  {
    //                      Id = relation.RelationType.Id,
    //                      Name = relation.RelationType.Name
    //                  },
    //                  PrimaryFieldId = relation.PrimaryFieldId,
    //                  PrimaryField = new FieldBasicResponseDto
    //                  {
    //                      Id = relation.PrimaryField.Id,
    //                      EntityId = relation.PrimaryField.EntityId,
    //                      Name = relation.PrimaryField.Name,
    //                      IsUnique = relation.PrimaryField.IsUnique,
    //                      FieldTypeId = relation.PrimaryField.FieldType.Id,
    //                      FieldType = new FieldTypeResponseDto
    //                      {
    //                          Id = relation.PrimaryField.FieldType.Id,
    //                          Name = relation.PrimaryField.FieldType.Name
    //                      }
    //                  },
    //                  ForeignFieldId = relation.ForeignFieldId,
    //                  ForeignField = new FieldBasicResponseDto
    //                  {
    //                      Id = relation.ForeignField.Id,
    //                      EntityId = relation.ForeignField.EntityId,
    //                      Name = relation.ForeignField.Name,
    //                      IsUnique = relation.ForeignField.IsUnique,
    //                      FieldTypeId = relation.ForeignField.FieldType.Id,
    //                      FieldType = new FieldTypeResponseDto
    //                      {
    //                          Id = relation.ForeignField.FieldType.Id,
    //                          Name = relation.ForeignField.FieldType.Name
    //                      }
    //                  }
    //              };

    //    return res.ToList();
    //}
}
