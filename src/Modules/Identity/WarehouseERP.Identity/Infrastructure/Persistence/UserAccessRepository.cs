using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class UserAccessRepository(DbContext db) : IUserAccessRepository
{
    public Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct) =>
        db.Set<UserAccess>()
            .Include(x => x.Branches)
            .Include(x => x.Warehouses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

    public void Add(UserAccess access) => db.Set<UserAccess>().Add(access);
}
