using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>A payment or refund as the cashier enters it.</summary>
/// <param name="Amount">For cash paid in: what the customer handed over (change comes out of it). Otherwise the amount.</param>
/// <param name="Reference">A card's authorization or reference number from the card terminal; required for cards.</param>
/// <param name="CardLast4">Optionally the card's last four digits; never the full number.</param>
public sealed record PosPaymentInput(PosPaymentMethod Method, decimal Amount, string? Reference, string? CardLast4);
