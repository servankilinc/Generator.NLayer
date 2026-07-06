using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class CrudTypeRepository : EFRepositoryBase<CrudType>
{
    public CrudTypeRepository(ProjectContext context) : base(context)
    {
    }
}
