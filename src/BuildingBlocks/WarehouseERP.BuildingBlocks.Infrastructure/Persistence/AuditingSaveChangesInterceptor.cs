using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

// Stamps who and when on every audited row (master data, Identity's users, roles and access), so commands never
// pass an actor around. Creation metadata can't be overwritten by a later update.
public sealed class AuditingSaveChangesInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? db)
    {
        if (db is null) return;
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var entry in db.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAtUtc)).CurrentValue = now;
                entry.Property(nameof(IAuditable.CreatedByUserId)).CurrentValue = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedByUserId)).IsModified = false;
                entry.Property(nameof(IAuditable.UpdatedAtUtc)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedByUserId)).CurrentValue = currentUser.UserId;
            }
        }
    }
}
