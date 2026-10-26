using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

// Stamps who and when on master data, so commands never pass an actor around. Creation metadata
// can't be overwritten by a later update.
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
        foreach (var entry in db.ChangeTracker.Entries<MasterEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedAtUtc).CurrentValue = now;
                entry.Property(x => x.CreatedByUserId).CurrentValue = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Property(x => x.CreatedByUserId).IsModified = false;
                entry.Property(x => x.UpdatedAtUtc).CurrentValue = now;
                entry.Property(x => x.UpdatedByUserId).CurrentValue = currentUser.UserId;
            }
        }
    }
}
