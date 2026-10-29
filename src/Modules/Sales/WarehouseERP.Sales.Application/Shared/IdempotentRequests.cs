using WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Shared;

// The first steps of every request that posts (a delivery, an invoice, a return's approval), inside its transaction: take
// the key's lock, so a retry racing the first attempt waits for its outcome, then look for the key's record. The record is
// added by the same save point as the posting, so after an uncertain commit the retry finds it and replays.
public sealed class IdempotentRequests(ISalesLocks locks, IIdempotencyStore records)
{
    /// <summary>The subject an earlier request with this key and fingerprint posted, null for a new request, or why not.</summary>
    public async Task<Result<Guid?>> BeginAsync(Guid key, string fingerprint, CancellationToken ct)
    {
        if (!await locks.TryLockRequestAsync(key, ct))
            return IdempotencyErrors.Busy;
        var previous = await records.FindAsync(key, ct);
        if (previous is null)
            return Result.Success<Guid?>(null);
        return previous.Fingerprint == fingerprint ? previous.SubjectId : IdempotencyErrors.KeyReused;
    }

    /// <summary>Remembers the request; saved by the caller's save point together with what it posted.</summary>
    public void Complete(Guid key, string operation, string fingerprint, Guid subjectId, Guid? userId, DateTime nowUtc) =>
        records.Add(IdempotencyRecord.Create(key, operation, fingerprint, subjectId, userId, nowUtc));
}
