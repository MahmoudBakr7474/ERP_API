using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Counts;

/// <param name="Reference">CT-000123.</param>
/// <param name="DifferenceCount">Lines whose counted quantity differs from the expected one.</param>
public sealed record StockCountSummaryResponse(
    Guid Id, string Reference, StockCountStatus Status, Guid WarehouseId, string WarehouseCode, bool IsWholeWarehouse,
    int LineCount, int CountedLineCount, int DifferenceCount, DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc);
