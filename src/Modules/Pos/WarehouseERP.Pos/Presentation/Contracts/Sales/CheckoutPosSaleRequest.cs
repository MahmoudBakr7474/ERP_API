using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Presentation.Contracts.Sales;

/// <summary>A cart and its payments; the Idempotency-Key header identifies the checkout.</summary>
public sealed record CheckoutPosSaleRequest(
    Guid TerminalId, Guid? CustomerId, string? CustomerName, string? CustomerPhone, string? Notes,
    IReadOnlyList<PosCartLineRequest> Lines, IReadOnlyList<PosPaymentRequest> Payments);
