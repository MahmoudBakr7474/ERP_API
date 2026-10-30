using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

// sp_getapplock owned by the current transaction: released by its commit or rollback, never held across requests.
internal sealed class AccountingLocks(DbContext db) : IAccountingLocks
{
    private const int TimeoutMilliseconds = 15_000;

    private const string Take =
        "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = @mode, @LockOwner = 'Transaction', @LockTimeout = @timeout";

    public Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct) =>
        TakeAsync($"accounting/request/{idempotencyKey:N}", "Exclusive", ct);

    public Task<bool> TryLockPartyAsync(Contracts.PartyType partyType, Guid partyId, CancellationToken ct) =>
        TakeAsync($"accounting/party/{(byte)partyType}/{partyId:N}", "Exclusive", ct);

    public Task<bool> TryLockCashAccountAsync(Guid cashAccountId, CancellationToken ct) =>
        TakeAsync($"accounting/cash/{cashAccountId:N}", "Exclusive", ct);

    public Task<bool> TryLockSourceAsync(Guid sourcePostingId, CancellationToken ct) =>
        TakeAsync($"accounting/source/{sourcePostingId:N}", "Exclusive", ct);

    public Task<bool> TryLockSourceScanAsync(CancellationToken ct) => TakeAsync("accounting/source-scan", "Exclusive", ct);

    public Task<bool> TryLockYearAsync(Guid fiscalYearId, CancellationToken ct) =>
        TakeAsync($"accounting/year/{fiscalYearId:N}", "Exclusive", ct);

    public Task<bool> TryLockPeriodAsync(Guid periodId, bool exclusive, CancellationToken ct) =>
        TakeAsync($"accounting/period/{periodId:N}", exclusive ? "Exclusive" : "Shared", ct);

    private async Task<bool> TakeAsync(string resource, string mode, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Accounting locks are owned by a transaction; start one first.");

        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Take,
        [
            result,
            new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource },
            new SqlParameter("@mode", SqlDbType.VarChar, 32) { Value = mode },
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
