namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// What a posted invoice means to the ledger. UnbilledAmount: What it takes off the unbilled value of its order lines:
/// the value its quantities had there.
/// </summary>
public sealed record SalesInvoiceAccounting(
    Guid Id, string Number, Guid CustomerId, Guid BranchId, DateOnly InvoiceDate, DateOnly DueDate, DateTime PostedAtUtc,
    decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal UnbilledAmount);
