using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// Goods received against one approved order, posted in one step: it is numbered, the order's lines record what came in
/// and Inventory posts a PurchaseReceipt stock document for it in the same transaction, which this receipt names (and
/// which names this receipt back). Immutable once saved; returns are separate documents. Each line keeps the order's
/// net unit price it entered stock at and how much of it went back to the supplier.
/// </summary>
public sealed class GoodsReceipt : IAuditable
{
    private readonly List<GoodsReceiptLine> _lines = [];

    private GoodsReceipt() { } // EF Core

    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
    public short FiscalYear { get; private set; }
    public int SequenceNumber { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>The supplier's delivery note or waybill number.</summary>
    public string? SupplierDeliveryNote { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>The PurchaseReceipt stock document Inventory posted for it.</summary>
    public Guid StockDocumentId { get; private set; }

    public string StockDocumentNumber { get; private set; } = null!;
    public DateTime ReceivedAtUtc { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<GoodsReceiptLine> Lines => _lines;

    /// <summary>
    /// The receipt of lines the order has just accepted (PurchaseOrder.Receive checked them). Its stock document is linked
    /// once Inventory posted it, before the save.
    /// </summary>
    public static GoodsReceipt Create(
        PurchaseOrder order, DocumentNumber number, IReadOnlyList<ReceiptLineInput> lines, string? supplierDeliveryNote,
        string? notes, Guid? userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        var receipt = new GoodsReceipt
        {
            Number = number.Number,
            FiscalYear = number.FiscalYear,
            SequenceNumber = number.SequenceNumber,
            PurchaseOrderId = order.Id,
            SupplierId = order.SupplierId,
            WarehouseId = order.WarehouseId,
            BranchId = order.BranchId,
            SupplierDeliveryNote = Text.Optional(supplierDeliveryNote),
            Notes = Text.Optional(notes),
            ReceivedAtUtc = nowUtc,
            ReceivedByUserId = userId,
        };
        for (var i = 0; i < lines.Count; i++)
        {
            var orderLine = order.FindLine(lines[i].OrderLineId)
                ?? throw new InvalidOperationException($"Order {order.Id} has no line {lines[i].OrderLineId}.");
            receipt._lines.Add(GoodsReceiptLine.Create(i + 1, orderLine, lines[i].Quantity, lines[i].LocationId));
        }
        return receipt;
    }

    public void LinkStockDocument(Guid stockDocumentId, string stockDocumentNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stockDocumentNumber);
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
    }

    public GoodsReceiptLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    /// <summary>Records what a supplier return sends back: never more of a line than it brought in and is still here.</summary>
    public Result RecordReturn(IReadOnlyList<SupplierReturnLine> returned)
    {
        foreach (var line in returned)
        {
            var receiptLine = FindLine(line.GoodsReceiptLineId)
                ?? throw new InvalidOperationException($"Receipt {Id} has no line {line.GoodsReceiptLineId}.");
            if (line.Quantity > receiptLine.ReturnableQuantity)
                return SupplierReturnDomainErrors.ExceedsReceipt(line.LineNumber, receiptLine.ReturnableQuantity);
        }
        foreach (var line in returned)
            FindLine(line.GoodsReceiptLineId)!.AddReturned(line.Quantity);
        return Result.Success();
    }
}
