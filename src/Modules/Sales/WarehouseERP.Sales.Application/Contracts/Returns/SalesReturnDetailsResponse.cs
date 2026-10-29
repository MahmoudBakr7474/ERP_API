using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Returns;

/// <param name="CostAmount">What the goods came back at: their delivery's cost, reversing the cost of goods sold.</param>
/// <param name="CreditTotalAmount">
/// What the company owes the customer for invoiced goods that came back: a pending credit for Accounting, not a credit
/// note or a refund.
/// </param>
/// <param name="CreatedByUserId">Its creator, who may also approve it.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SalesReturnDetailsResponse(
    Guid Id, string DraftReference, string? Number, SalesReturnStatus Status, Guid DeliveryId, string? DeliveryNumber, Guid OrderId,
    string? OrderNumber, Guid CustomerId, string CustomerCode, string CustomerName, Guid WarehouseId, string WarehouseCode,
    Guid BranchId, string CurrencyCode, string Reason, string? Notes, decimal CostAmount, decimal CreditNetAmount,
    decimal CreditTaxAmount, decimal CreditTotalAmount, Guid? StockDocumentId, string? StockDocumentNumber, DateTime CreatedAtUtc,
    Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy, DateTime? ApprovedAtUtc, string RowVersion,
    IReadOnlyList<SalesReturnLineResponse> Lines, IReadOnlyList<SalesDocumentEventResponse> Events);
