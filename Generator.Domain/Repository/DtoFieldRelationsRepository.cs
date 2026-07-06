using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class DtoFieldRelationsRepository : EFRepositoryBase<DtoFieldRelations>
{
    public DtoFieldRelationsRepository(ProjectContext context) : base(context)
    {
    }
}
