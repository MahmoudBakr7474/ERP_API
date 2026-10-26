using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Contracts;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class UserDirectory(DbContext db) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string>();
        var ids = userIds.Distinct().ToArray();
        return await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
    }
}
