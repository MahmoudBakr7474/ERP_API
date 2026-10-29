using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Presentation.Contracts.Returns;

/// <summary>Goods coming back against a sale and their refunds; the Idempotency-Key header identifies the return.</summary>
public sealed record CreatePosReturnRequest(
    Guid TerminalId, Guid SaleId, string? Reason, string? Notes, IReadOnlyList<PosReturnLineRequest> Lines,
    IReadOnlyList<PosPaymentRequest> Refunds);
