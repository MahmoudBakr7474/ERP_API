using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// One way a return was refunded: cash out of the shift's drawer, or a card refund made on a standalone card terminal and
/// recorded with its reference (POS does not settle it electronically). Never changes.
/// </summary>
public sealed class PosRefund
{
    private PosRefund() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public PosPaymentMethod Method { get; private set; }
    public PosPaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public string? CardLast4 { get; private set; }

    internal static PosRefund Create(int lineNumber, PosPaymentMethod method, decimal amount, string? reference, string? last4) => new()
    {
        LineNumber = lineNumber,
        Method = method,
        Status = method == PosPaymentMethod.Cash ? PosPaymentStatus.Settled : PosPaymentStatus.ExternallyConfirmed,
        Amount = amount,
        Reference = reference,
        CardLast4 = method == PosPaymentMethod.Card ? last4 : null,
    };
}
