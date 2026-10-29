using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Application.Features.Sales.CheckoutPosSale;

/// <summary>Sells a cart at a terminal on the caller's open shift, paid as entered.</summary>
/// <param name="CustomerId">A registered (active) customer; null for a walk-in.</param>
/// <param name="CustomerName">A walk-in's name for the receipt; ignored for a registered customer.</param>
/// <param name="CustomerPhone">A walk-in's phone for the receipt; ignored for a registered customer.</param>
/// <param name="IdempotencyKey">From the Idempotency-Key header: a new GUID per checkout, sent again unchanged on a retry.</param>
public sealed record CheckoutPosSaleCommand(
    Guid TerminalId, Guid? CustomerId, string? CustomerName, string? CustomerPhone, string? Notes,
    IReadOnlyList<PosCartLineRequest> Lines, IReadOnlyList<PosPaymentRequest> Payments, Guid IdempotencyKey);
