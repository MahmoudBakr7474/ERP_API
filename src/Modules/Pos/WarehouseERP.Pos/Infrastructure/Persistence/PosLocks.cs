using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

// sp_getapplock owned by the current transaction: released by its commit or rollback, never held across requests.
internal sealed class PosLocks(DbContext db) : IPosLocks
{
    private const int TimeoutMilliseconds = 15_000;

    private const string Take =
        "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout";

    public async Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("POS request locks are owned by a transaction; start one first.");

        var resource = $"pos/request/{idempotencyKey:N}";
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Take,
        [
            result,
            new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource },
            new SqlParameter("@timeout", SqlDbType.Int) { Value = TimeoutMilliseconds },
        ], ct);

        var code = (int)result.Value;
        if (code == -3)
            throw new TransientDatabaseException($"Chosen as deadlock victim while waiting for {resource}.");
        if (code < -1)
            throw new InvalidOperationException($"sp_getapplock refused {resource} with code {code}.");
        return code >= 0;
    }
}
