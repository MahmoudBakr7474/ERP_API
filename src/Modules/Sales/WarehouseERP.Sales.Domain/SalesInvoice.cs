using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// What a customer owes for delivered goods of one sales order: each line bills an order line's quantity at its agreed
/// price, discount and VAT (copied from the order, never re-priced), at most what was delivered and not invoiced or
/// returned before invoicing yet. An order may have several invoices. Posting numbers it, fixes its due date (invoice
/// date plus the order's payment terms) and records it on the order; posted invoices never change. No payments, receivable
/// ledger or general ledger in V1: the totals, tax and due date wait for Accounting.
/// </summary>
public sealed class SalesInvoice : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<SalesInvoiceLine> _lines = [];
    private readonly List<SalesInvoiceEvent> _events = [];

    private SalesInvoice() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DINV-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public SalesInvoiceStatus Status { get; private set; }
    public DateOnly InvoiceDate { get; private set; }

    /// <summary>Days from invoice date to due date, copied from the order.</summary>
    public int PaymentTermDays { get; private set; }

    public DateOnly DueDate { get; private set; }

    /// <summary>The customer's reference (their purchase order), copied from the order unless given.</summary>
    public string? CustomerReference { get; private set; }

    public string? Notes { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SalesInvoiceLine> Lines => _lines;
    public IReadOnlyList<SalesInvoiceEvent> Events => _events;

    public static Result<SalesInvoice> CreateDraft(
        SalesOrder order, DateOnly invoiceDate, string? customerReference, string? notes, IReadOnlyList<InvoiceLineInput> lines,
        Guid? userId, DateTime nowUtc)
    {
        var invoice = new SalesInvoice
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            WarehouseId = order.WarehouseId,
            BranchId = order.BranchId,
            CurrencyCode = order.CurrencyCode,
            PaymentTermDays = order.PaymentTermDays,
            Status = SalesInvoiceStatus.Draft,
        };
        var filled = invoice.Fill(order, invoiceDate, customerReference, notes, lines);
        if (filled.IsFailure)
            return filled.Error;
        invoice.Record(SalesDocumentAction.Created, userId, nowUtc, note: null);
        return invoice;
    }

    /// <summary>Replaces a draft's date, references and lines.</summary>
    public Result ReplaceDraft(
        SalesOrder order, DateOnly invoiceDate, string? customerReference, string? notes, IReadOnlyList<InvoiceLineInput> lines) =>
        Status != SalesInvoiceStatus.Draft ? SalesInvoiceDomainErrors.NotDraft : Fill(order, invoiceDate, customerReference, notes, lines);

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesInvoiceStatus.Draft)
            return SalesInvoiceDomainErrors.NotDraft;
        Status = SalesInvoiceStatus.Cancelled;
        Record(SalesDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>What it bills per order line, for the order.</summary>
    public IReadOnlyList<InvoiceMatch> Matches() =>
        _lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceMatch(l.OrderLineId, l.Quantity, l.NetAmount, l.TaxAmount)).ToList();

    public Result MarkPosted(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesInvoiceStatus.Draft)
            return SalesInvoiceDomainErrors.NotDraft;
        Status = SalesInvoiceStatus.Posted;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        Record(SalesDocumentAction.Posted, userId, nowUtc, note: null);
        return Result.Success();
    }

    private Result Fill(SalesOrder order, DateOnly invoiceDate, string? customerReference, string? notes, IReadOnlyList<InvoiceLineInput> lines)
    {
        if (lines.Count == 0)
            return SalesInvoiceDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SalesInvoiceDomainErrors.TooManyLines;
        var allowed = order.CanInvoice(lines.Select(l => new OrderLineQuantity(l.OrderLineId, l.Quantity)).ToList());
        if (allowed.IsFailure)
            return allowed.Error;

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var built = new List<SalesInvoiceLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = SalesInvoiceLine.Create(i + 1, lines[i], order.FindLine(lines[i].OrderLineId)!, decimals);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        InvoiceDate = invoiceDate;
        DueDate = invoiceDate.AddDays(PaymentTermDays);
        CustomerReference = Text.Optional(customerReference) ?? order.CustomerReference;
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        GrossAmount = built.Sum(l => l.GrossAmount);
        DiscountAmount = built.Sum(l => l.DiscountAmount);
        NetAmount = built.Sum(l => l.NetAmount);
        TaxAmount = built.Sum(l => l.TaxAmount);
        TotalAmount = built.Sum(l => l.TotalAmount);
        return Result.Success();
    }

    private void Record(SalesDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SalesInvoiceEvent.Create(action, userId, nowUtc, note));
}
