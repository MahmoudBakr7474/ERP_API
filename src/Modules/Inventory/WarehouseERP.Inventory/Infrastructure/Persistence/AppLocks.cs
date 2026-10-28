using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

// sp_getapplock owned by the current transaction: released by its commit or rollback, never held across requests.
internal static class AppLocks
{
    private const int TimeoutMilliseconds = 15_000;

    private const string Take =
        "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = @mode, @LockOwner = 'Transaction', @LockTimeout = @timeout";

    public static string Warehouse(Guid warehouseId) => $"inventory/warehouse/{warehouseId:N}";

    public static string Request(Guid idempotencyKey) => $"inventory/request/{idempotencyKey:N}";

    /// <summary>
    /// Takes the lock; false when it was not granted in time. Being chosen as a deadlock victim is transient: the whole
    /// transaction runs again (EfTransactionRunner).
    /// </summary>
    public static async Task<bool> TryTakeAsync(DbContext db, string resource, bool exclusive, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Inventory locks are owned by a transaction; start one first.");

        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Take,
        [
            result,
            new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource },
            new SqlParameter("@mode", SqlDbType.VarChar, 32) { Value = exclusive ? "Exclusive" : "Shared" },
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
