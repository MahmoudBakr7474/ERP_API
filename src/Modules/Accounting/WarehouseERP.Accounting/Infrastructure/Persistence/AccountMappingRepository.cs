using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class AccountMappingRepository(DbContext db) : IAccountMappingRepository
{
    public async Task<IReadOnlyList<AccountMapping>> ListAsync(CancellationToken ct) =>
        await db.Set<AccountMapping>().AsNoTracking().OrderBy(x => x.Role).ToListAsync(ct);

    public Task<AccountMapping?> FindAsync(PostingRole role, CancellationToken ct) =>
        db.Set<AccountMapping>().AsNoTracking().SingleOrDefaultAsync(x => x.Role == role, ct);

    public async Task<AccountMapping?> GetForUpdateAsync(PostingRole role, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var mapping = await db.Set<AccountMapping>().SingleOrDefaultAsync(x => x.Role == role, ct);
        if (mapping is not null && expectedRowVersion is not null)
            db.Entry(mapping).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return mapping;
    }

    public Task<bool> AnyAsync(CancellationToken ct) => db.Set<AccountMapping>().AnyAsync(ct);

    public void Add(AccountMapping mapping) => db.Add(mapping);

    public void Remove(AccountMapping mapping) => db.Remove(mapping);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
