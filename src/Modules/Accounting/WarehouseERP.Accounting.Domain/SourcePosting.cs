using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// The accounting identity of one operational document (unique by type and id, so no document is ever journaled twice) and
/// where its journal stands: found (pending), posted with its journal, failed with the reason and when it is tried again, or
/// nothing to post. A failure never loses the document: it stays visible and is tried again until its journal posts. The
/// stock values compare what Inventory's cost entries moved (exact) with what the journal put on the inventory account.
/// </summary>
public sealed class SourcePosting
{
    public const int MaxNumberLength = 50;
    public const int MaxErrorCodeLength = 100;
    public const int MaxErrorMessageLength = 1000;

    /// <summary>A failure is tried again after 1, 2, 4 ... minutes, at most this long apart.</summary>
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(1);

    private SourcePosting() { } // EF Core

    public Guid Id { get; private set; }
    public SourceDocumentType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public string SourceNumber { get; private set; } = null!;

    /// <summary>When the document posted in its module.</summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Its accounting date: the company's local date of <see cref="OccurredAtUtc"/>.</summary>
    public DateOnly PostingDate { get; private set; }

    public SourcePostingStatus Status { get; private set; }
    public Guid? JournalId { get; private set; }
    public string? JournalNumber { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }

    /// <summary>When the worker takes it next; null once it posted or had nothing to post.</summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>What Inventory's cost entries of its stock document moved, exactly (signed: in positive).</summary>
    public decimal? StockValue { get; private set; }

    /// <summary>What its journal put on the inventory account (signed: debit positive).</summary>
    public decimal? PostedStockValue { get; private set; }

    /// <summary>A supplier price or cost difference its journal kept for the accountant's review (debit positive).</summary>
    public decimal? VarianceAmount { get; private set; }

    public DateTime DiscoveredAtUtc { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsSettled => Status is SourcePostingStatus.Posted or SourcePostingStatus.NothingToPost;

    public static SourcePosting Discover(
        SourceDocumentType sourceType, Guid sourceId, string sourceNumber, DateTime occurredAtUtc, DateOnly postingDate, DateTime nowUtc)
    {
        if (!Enum.IsDefined(sourceType))
            throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Unknown source document type.");
        return new SourcePosting
        {
            SourceType = sourceType,
            SourceId = sourceId,
            SourceNumber = Clip(sourceNumber, MaxNumberLength)!,
            OccurredAtUtc = occurredAtUtc,
            PostingDate = postingDate,
            Status = SourcePostingStatus.Pending,
            NextAttemptAtUtc = nowUtc,
            DiscoveredAtUtc = nowUtc,
        };
    }

    public void MarkPosted(Guid journalId, string journalNumber, decimal? stockValue, decimal? postedStockValue, decimal? varianceAmount, DateTime nowUtc)
    {
        if (IsSettled)
            throw new InvalidOperationException($"Source posting {Id} is already {Status}.");
        Status = SourcePostingStatus.Posted;
        JournalId = journalId;
        JournalNumber = journalNumber;
        StockValue = stockValue;
        PostedStockValue = postedStockValue;
        VarianceAmount = varianceAmount;
        Settle(nowUtc);
    }

    public void MarkNothingToPost(decimal? stockValue, DateTime nowUtc)
    {
        if (IsSettled)
            throw new InvalidOperationException($"Source posting {Id} is already {Status}.");
        Status = SourcePostingStatus.NothingToPost;
        StockValue = stockValue;
        PostedStockValue = stockValue is null ? null : 0m;
        Settle(nowUtc);
    }

    /// <summary>Records why it failed and when it is tried again (doubling the wait each time, up to an hour).</summary>
    public void MarkFailed(string errorCode, string errorMessage, DateTime nowUtc)
    {
        if (IsSettled)
            throw new InvalidOperationException($"Source posting {Id} is already {Status}.");
        Status = SourcePostingStatus.Failed;
        Attempts++;
        LastAttemptAtUtc = nowUtc;
        ErrorCode = Clip(errorCode, MaxErrorCodeLength);
        ErrorMessage = Clip(errorMessage, MaxErrorMessageLength);
        var minutes = Math.Pow(2, Math.Min(Attempts - 1, 10));
        var delay = TimeSpan.FromMinutes(minutes);
        NextAttemptAtUtc = nowUtc + (delay > MaxRetryDelay ? MaxRetryDelay : delay);
    }

    private void Settle(DateTime nowUtc)
    {
        Attempts++;
        LastAttemptAtUtc = nowUtc;
        PostedAtUtc = nowUtc;
        NextAttemptAtUtc = null;
        ErrorCode = null;
        ErrorMessage = null;
    }

    private static string? Clip(string? text, int max) =>
        text is null ? null : text.Length <= max ? text : text[..max];
}
