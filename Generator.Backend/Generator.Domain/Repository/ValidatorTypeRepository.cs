using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class ValidatorTypeRepository : EFRepositoryBase<ValidatorType>
{
    public ValidatorTypeRepository(ProjectContext context) : base(context)
    {
    }
}
