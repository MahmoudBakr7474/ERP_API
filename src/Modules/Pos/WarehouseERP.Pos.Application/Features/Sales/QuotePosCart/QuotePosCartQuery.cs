using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;

/// <summary>Prices a cart for a terminal without recording anything: ask it before taking a card payment.</summary>
public sealed record QuotePosCartQuery(Guid TerminalId, IReadOnlyList<PosCartLineRequest> Lines);
