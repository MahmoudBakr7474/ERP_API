using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

/// <summary>
/// The journal an operational document calls for, before its posting roles are resolved to accounts: every line in the
/// document's branch. StockValue is what Inventory's cost entries moved (exact), PostedStockValue what the inventory account
/// gets; VarianceAmount a supplier price or cost difference kept for the accountant's review.
/// </summary>
public sealed record SourceEntry(
    JournalType JournalType, Guid BranchId, string Description, string Reference, IReadOnlyList<SourceLine> Lines,
    decimal? StockValue, decimal? PostedStockValue, decimal? VarianceAmount);
