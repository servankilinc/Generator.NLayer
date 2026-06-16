using {{ core_project_name }}.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using {{ core_project_name }}.Utils.HttpContextManager;

namespace {{ dataAccess_project_name }}.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextManager _httpContextManager;
    public AuditInterceptor(IHttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    #region SYNC VERSION
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<IAuditableEntity>> auditableEntries = eventData.Context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is not IProjectEntity);

        if (auditableEntries.Any())
        {
            var requesterId = _httpContextManager.GetNameIdentifier();

            foreach (EntityEntry<IAuditableEntity> entry in auditableEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                    entry.Entity.CreateDateUtc = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                    entry.Entity.UpdateDateUtc = DateTime.UtcNow;

                    entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreateDateUtc)).IsModified = false;
                }
            }
        }

        return base.SavingChanges(eventData, result);
    }
    #endregion


    #region ASYNC VERSION
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<IAuditableEntity>> auditableEntries = eventData.Context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is not IProjectEntity);

        if (auditableEntries.Any())
        {
            var requesterId = _httpContextManager.GetNameIdentifier();

            foreach (EntityEntry<IAuditableEntity> entry in auditableEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                    entry.Entity.CreateDateUtc = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = requesterId.IsSuccess ? requesterId.Data : string.Empty;
                    entry.Entity.UpdateDateUtc = DateTime.UtcNow;

                    entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreateDateUtc)).IsModified = false;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    #endregion
}
