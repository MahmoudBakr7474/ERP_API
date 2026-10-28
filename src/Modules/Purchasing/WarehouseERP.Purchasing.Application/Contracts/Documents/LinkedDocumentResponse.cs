using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Documents;

/// <summary>A document linked to the one shown: an order's receipts, invoices and returns, or a receipt's returns.</summary>
/// <param name="Reference">Its number, or its draft reference before it has one.</param>
/// <param name="Status">Its status name (each document type has its own statuses).</param>
/// <param name="Amount">An invoice's total or a return's credit claim; null for a receipt.</param>
public sealed record LinkedDocumentResponse(
    Guid Id, PurchasingDocumentType DocumentType, string Reference, string Status, DateTime AtUtc, decimal? Amount);
