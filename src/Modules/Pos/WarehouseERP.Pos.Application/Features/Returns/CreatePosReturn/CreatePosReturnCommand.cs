using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Application.Features.Returns.CreatePosReturn;

/// <summary>Takes goods back against a POS sale at a terminal of its warehouse, on the caller's open shift, and refunds them.</summary>
/// <param name="Refunds">Adding up to exactly what the returned goods are worth; each back the way the sale was paid.</param>
/// <param name="IdempotencyKey">From the Idempotency-Key header: a new GUID per return, sent again unchanged on a retry.</param>
public sealed record CreatePosReturnCommand(
    Guid TerminalId, Guid SaleId, string? Reason, string? Notes, IReadOnlyList<PosReturnLineRequest> Lines,
    IReadOnlyList<PosPaymentRequest> Refunds, Guid IdempotencyKey);
