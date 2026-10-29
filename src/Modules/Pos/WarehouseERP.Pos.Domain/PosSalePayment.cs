using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// One way a sale was paid. Cash: the amount it settled (tendered less change), what was tendered and the change given.
/// Card: the amount confirmed on a standalone card terminal, its reference and optionally the card's last four digits.
/// Never changes.
/// </summary>
public sealed class PosSalePayment
{
    private PosSalePayment() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public int LineNumber { get; private set; }
    public PosPaymentMethod Method { get; private set; }
    public PosPaymentStatus Status { get; private set; }

    /// <summary>What it paid of the sale.</summary>
    public decimal Amount { get; private set; }

    /// <summary>What the customer handed over (cash), or the amount (card).</summary>
    public decimal TenderedAmount { get; private set; }

    public decimal ChangeAmount { get; private set; }
    public string? Reference { get; private set; }
    public string? CardLast4 { get; private set; }

    internal static PosSalePayment Cash(int lineNumber, decimal received, decimal tendered, decimal change) => new()
    {
        LineNumber = lineNumber, Method = PosPaymentMethod.Cash, Status = PosPaymentStatus.Settled, Amount = received,
        TenderedAmount = tendered, ChangeAmount = change,
    };

    internal static PosSalePayment Card(int lineNumber, decimal amount, string reference, string? last4) => new()
    {
        LineNumber = lineNumber, Method = PosPaymentMethod.Card, Status = PosPaymentStatus.ExternallyConfirmed, Amount = amount,
        TenderedAmount = amount, Reference = reference, CardLast4 = last4,
    };
}
