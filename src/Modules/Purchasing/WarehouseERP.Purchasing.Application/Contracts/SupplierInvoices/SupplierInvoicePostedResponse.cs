namespace WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

/// <summary>What posting (or approving) an invoice did; a retry with the same Idempotency-Key gets the same answer.</summary>
/// <param name="Number">SIN-…, the company's own number.</param>
public sealed record SupplierInvoicePostedResponse(
    Guid Id, string Number, decimal TotalAmount, decimal PriceVarianceAmount, DateOnly DueDate, DateTime PostedAtUtc);
