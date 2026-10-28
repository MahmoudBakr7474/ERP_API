namespace WarehouseERP.Purchasing.Domain;

/// <summary>What an invoice says besides its order and lines.</summary>
/// <param name="SupplierInvoiceNumber">The supplier's invoice number as printed.</param>
/// <param name="DueDate">When it must be paid; the invoice date plus the order's payment terms when left out.</param>
public sealed record InvoiceTerms(string SupplierInvoiceNumber, DateOnly InvoiceDate, DateOnly? DueDate, string? Notes);
