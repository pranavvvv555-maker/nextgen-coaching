using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Interfaces;

namespace NextGenCoaching.Core.Persistence.Interceptors;

/// <summary>
/// Populates CreatedAtUtc/UpdatedAtUtc and CreatedBy/UpdatedBy automatically
/// on every SaveChanges call. Reads the current user from ICurrentUserService.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableEntityInterceptor(ICurrentUserService currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable auditable) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAtUtc = now;
                    auditable.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    auditable.UpdatedAtUtc = now;
                    auditable.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    // Convert hard-delete to soft-delete for ISoftDeletable
                    if (entry.Entity is ISoftDeletable soft)
                    {
                        entry.State = EntityState.Modified;
                        soft.IsDeleted = true;
                        auditable.UpdatedAtUtc = now;
                        auditable.UpdatedBy = userId;
                    }
                    break;
            }
        }
    }
}
