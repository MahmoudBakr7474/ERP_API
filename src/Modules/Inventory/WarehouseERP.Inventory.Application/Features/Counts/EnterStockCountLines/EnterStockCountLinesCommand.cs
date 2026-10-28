namespace WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;

/// <summary>Counted quantities; a line for a product and location not snapshotted is added, expecting zero.</summary>
public sealed record EnterStockCountLinesCommand(Guid CountId, IReadOnlyList<CountedLineRequest> Lines, string RowVersion);
