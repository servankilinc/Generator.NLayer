using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class ValidatorTypeParamRepository : EFRepositoryBase<ValidatorTypeParam>
{
    public ValidatorTypeParamRepository(ProjectContext context) : base(context)
    {
    }
}