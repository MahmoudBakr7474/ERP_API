namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A request that posts stock or a liability, by the key its client sent and a fingerprint of the request. Saved in the
/// same transaction as its change: a retry with the same key and request gets the first outcome back instead of a second
/// posting, and the same key with another request is refused.
/// </summary>
public sealed class IdempotencyRecord
{
    private IdempotencyRecord() { } // EF Core

    public Guid Key { get; private set; }
    public string Operation { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;

    /// <summary>The goods receipt, supplier invoice or supplier return the request posted.</summary>
    public Guid SubjectId { get; private set; }

    public Guid? UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static IdempotencyRecord Create(Guid key, string operation, string fingerprint, Guid subjectId, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        return new IdempotencyRecord
        {
            Key = key, Operation = operation, Fingerprint = fingerprint, SubjectId = subjectId, UserId = userId, CreatedAtUtc = nowUtc,
        };
    }
}
