using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Presentation.Contracts.Sales;

/// <summary>A cart to price for a terminal without recording anything.</summary>
public sealed record QuotePosCartRequest(Guid TerminalId, IReadOnlyList<PosCartLineRequest> Lines);
