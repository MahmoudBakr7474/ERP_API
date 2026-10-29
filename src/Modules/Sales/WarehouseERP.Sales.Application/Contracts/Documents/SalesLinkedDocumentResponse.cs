using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Documents;

/// <summary>A document linked to the one shown: an order's deliveries, invoices and returns, or a delivery's returns.</summary>
/// <param name="Reference">Its number, or its draft reference before it has one.</param>
/// <param name="Status">Its status name (each document type has its own statuses).</param>
/// <param name="Amount">An invoice's total or a return's pending credit; a delivery's cost.</param>
public sealed record SalesLinkedDocumentResponse(
    Guid Id, SalesDocumentType DocumentType, string Reference, string Status, DateTime AtUtc, decimal? Amount);
