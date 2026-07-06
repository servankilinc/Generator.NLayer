using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Generator.Domain.Context;

namespace Generator.Domain.Repository;

public class AppSettingsRepository : EFRepositoryBase<AppSetting>
{
    public AppSettingsRepository(ProjectContext context) : base(context)
    {
    }
}
