using {{ core_project_name }}.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using {{ core_project_name }}.Utils.HttpContextManager;

namespace {{ dataAccess_project_name }}.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextManager _httpContextManager;
    public SoftDeleteInterceptor(IHttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    #region SYNC VERSION
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<ISoftDeletableEntity>> softDeletableEntries = eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted && e.Entity is not IProjectEntity);

        if (softDeletableEntries.Any())
        {
            var requesterId = _httpContextManager.GetNameIdentifier();

            foreach (EntityEntry<ISoftDeletableEntity> entry in softDeletableEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedDateUtc = DateTime.UtcNow;
            }
        }

        return base.SavingChanges(eventData, result);
    }
    #endregion


    #region ASYNC VERSION
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<ISoftDeletableEntity>> softDeletableEntries = eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted && e.Entity is not IProjectEntity);

        if (softDeletableEntries.Any())
        {
            var requesterId = _httpContextManager.GetNameIdentifier();

            foreach (EntityEntry<ISoftDeletableEntity> entry in softDeletableEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedDateUtc = DateTime.UtcNow;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    #endregion
}
