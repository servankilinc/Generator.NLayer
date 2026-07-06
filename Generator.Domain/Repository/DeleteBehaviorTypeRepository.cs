using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class DeleteBehaviorTypeRepository : EFRepositoryBase<DeleteBehaviorType>
{
    public DeleteBehaviorTypeRepository(ProjectContext context) : base(context)
    {
    }
}
