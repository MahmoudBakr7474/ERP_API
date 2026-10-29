using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <summary>A payment (checkout) or refund (return) as the cashier enters it.</summary>
/// <param name="Amount">Cash paid in: what the customer handed over. Otherwise the amount.</param>
/// <param name="Reference">The card terminal's authorization or reference number; required for cards.</param>
/// <param name="CardLast4">Optionally the card's last four digits; never the full card number.</param>
public sealed record PosPaymentRequest(PosPaymentMethod Method, decimal Amount, string? Reference, string? CardLast4);
