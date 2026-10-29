using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Orders;

/// <param name="ShippingAddress">The delivery address as text, copied from the customer when the order was made.</param>
/// <param name="InvoicedAmount">Total of its posted invoices.</param>
/// <param name="CreditedAmount">Credits owed to the customer for invoiced goods that came back, pending Accounting.</param>
/// <param name="ApprovalReasons">Why it went to an approver; empty when confirmed directly.</param>
/// <param name="CreatedByUserId">Its creator, who may also approve it.</param>
/// <param name="Documents">Its deliveries, invoices and returns, oldest first.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SalesOrderDetailsResponse(
    Guid Id, string DraftReference, string? Number, SalesOrderStatus Status, Guid CustomerId, string CustomerCode, string CustomerName,
    Guid WarehouseId, string WarehouseCode, Guid BranchId, string CurrencyCode, DateOnly OrderDate, DateOnly? RequestedDeliveryDate,
    string? CustomerReference, int PaymentTermDays, string? ShippingAddress, string? Notes, Guid? SourceQuotationId,
    string? SourceQuotationNumber, decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount,
    decimal TotalAmount, decimal InvoicedAmount, decimal CreditedAmount, IReadOnlyList<ApprovalReason> ApprovalReasons,
    string? ClosedReason, DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy,
    DateTime? ApprovedAtUtc, DateTime? ConfirmedAtUtc, string RowVersion, IReadOnlyList<SalesOrderLineResponse> Lines,
    IReadOnlyList<SalesLinkedDocumentResponse> Documents, IReadOnlyList<SalesDocumentEventResponse> Events);
