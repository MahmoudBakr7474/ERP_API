using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.SourcePostings;

/// <summary>An operational document's accounting identity and where its journal stands.</summary>
/// <param name="PostingDate">Its accounting date: the company's local date of the moment it posted in its module.</param>
/// <param name="NextAttemptAtUtc">When the worker tries it next; null once it posted or had nothing to post.</param>
/// <param name="StockValue">What Inventory's cost entries of its stock document moved, exactly (in positive).</param>
/// <param name="PostedStockValue">What its journal put on the inventory account (debit positive).</param>
/// <param name="VarianceAmount">A supplier price or cost difference its journal kept for the accountant's review.</param>
public sealed record SourcePostingResponse(
    Guid Id, SourceDocumentType SourceType, Guid SourceId, string SourceNumber, DateTime OccurredAtUtc, DateOnly PostingDate,
    SourcePostingStatus Status, Guid? JournalId, string? JournalNumber, int Attempts, DateTime? LastAttemptAtUtc,
    DateTime? NextAttemptAtUtc, string? ErrorCode, string? ErrorMessage, decimal? StockValue, decimal? PostedStockValue,
    decimal? VarianceAmount, DateTime DiscoveredAtUtc, DateTime? PostedAtUtc);
