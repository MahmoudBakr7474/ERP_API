using WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;

namespace WarehouseERP.Inventory.Presentation.Contracts.Counts;

public sealed record EnterStockCountLinesRequest(IReadOnlyList<CountedLineRequest> Lines, string RowVersion);
