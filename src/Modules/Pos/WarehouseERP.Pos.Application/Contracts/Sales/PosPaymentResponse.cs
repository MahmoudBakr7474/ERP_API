using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <param name="Amount">What it paid of the sale (cash: tendered less change).</param>
/// <param name="TenderedAmount">What the customer handed over (cash) or the amount (card).</param>
public sealed record PosPaymentResponse(
    int LineNumber, PosPaymentMethod Method, PosPaymentStatus Status, decimal Amount, decimal TenderedAmount, decimal ChangeAmount,
    string? Reference, string? CardLast4);
