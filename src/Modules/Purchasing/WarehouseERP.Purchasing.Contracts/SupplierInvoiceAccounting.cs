namespace WarehouseERP.Purchasing.Contracts;

/// <summary>
/// What a posted supplier invoice means to the ledger. ReceivedValue: What it takes off goods received not invoiced:
/// the value its quantities had at the order's net unit price. PriceVarianceAmount: Purchasing's own price variance
/// (invoice net price minus order net price, per line).
/// </summary>
public sealed record SupplierInvoiceAccounting(
    Guid Id, string Number, string SupplierInvoiceNumber, Guid SupplierId, Guid BranchId, DateOnly InvoiceDate, DateOnly DueDate,
    DateTime PostedAtUtc, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal ReceivedValue,
    decimal PriceVarianceAmount);
