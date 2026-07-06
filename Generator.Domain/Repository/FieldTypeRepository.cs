using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class FieldTypeRepository : EFRepositoryBase<FieldType>
{
    public FieldTypeRepository(ProjectContext context) : base(context)
    {
    }
}
