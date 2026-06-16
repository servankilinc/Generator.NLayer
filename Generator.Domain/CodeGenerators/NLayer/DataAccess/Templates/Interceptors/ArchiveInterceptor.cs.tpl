using {{ core_project_name }}.Model;
using {{ core_project_name }}.Utils.HttpContextManager;
using {{ dataAccess_project_name }}.Interceptors.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using {{ model_project_name }}.ProjectEntities;

namespace {{ dataAccess_project_name }}.Interceptors;

public sealed class ArchiveInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextManager _httpContextManager;
    public ArchiveInterceptor(IHttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    #region SYNC VERSION
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<IArchivableEntity>> archivableEntries = eventData.Context.ChangeTracker.Entries<IArchivableEntity>()
            .Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (archivableEntries.Any())
        {
            List<Archive> archives = new List<Archive>();

            var requesterId = _httpContextManager.GetNameIdentifier();
            var clientIp = _httpContextManager.GetClientIp();
            var userAgent = _httpContextManager.GetUserAgent();

            foreach (EntityEntry<IArchivableEntity> entry in archivableEntries)
            {
                archives.Add(new Archive
                {
                    TableName = entry.GetTableName(),
                    EntityId = entry.GetEntityId(),
                    RequesterId = requesterId.IsSuccess ? requesterId.Data : string.Empty,
                    ClientIp = clientIp.IsSuccess ? clientIp.Data : string.Empty,
                    UserAgent = userAgent.IsSuccess ? userAgent.Data : string.Empty,
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                    Data = entry.GetOriginalData()
                });
            }
            eventData.Context.Set<Archive>().AddRange(archives);
        }

        return base.SavingChanges(eventData, result);
    }
    #endregion


    #region ASYNC VERSION
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<IArchivableEntity>> archivableEntries = eventData.Context.ChangeTracker.Entries<IArchivableEntity>()
            .Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (archivableEntries.Any())
        {
            List<Archive> archives = new List<Archive>();

            var requesterId = _httpContextManager.GetNameIdentifier();
            var clientIp = _httpContextManager.GetClientIp();
            var userAgent = _httpContextManager.GetUserAgent();

            foreach (EntityEntry<IArchivableEntity> entry in archivableEntries)
            {
                archives.Add(new Archive
                {
                    TableName = entry.GetTableName(),
                    EntityId = entry.GetEntityId(),
                    RequesterId = requesterId.IsSuccess ? requesterId.Data : string.Empty,
                    ClientIp = clientIp.IsSuccess ? clientIp.Data : string.Empty,
                    UserAgent = userAgent.IsSuccess ? userAgent.Data : string.Empty,
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                    Data = entry.GetOriginalData()
                });
            }
            eventData.Context.Set<Archive>().AddRange(archives);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    #endregion
}
