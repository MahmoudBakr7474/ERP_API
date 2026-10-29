using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Quotations;

/// <param name="IsExpired">Issued and past its validity date: it can't be converted any more.</param>
/// <param name="ApprovalReasons">Why it went to an approver; empty when issued directly.</param>
/// <param name="CreatedByUserId">Its creator, who may also approve it.</param>
/// <param name="ConvertedOrderId">The sales order it became.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SalesQuotationDetailsResponse(
    Guid Id, string DraftReference, string? Number, SalesQuotationStatus Status, bool IsExpired, Guid CustomerId, string CustomerCode,
    string CustomerName, Guid WarehouseId, string WarehouseCode, Guid BranchId, string CurrencyCode, DateOnly QuotationDate,
    DateOnly ValidUntil, string? CustomerReference, int PaymentTermDays, string? Notes, decimal GrossAmount, decimal DiscountAmount,
    decimal NetAmount, decimal TaxAmount, decimal TotalAmount, IReadOnlyList<ApprovalReason> ApprovalReasons, string? DeclinedReason,
    Guid? ConvertedOrderId, DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy,
    DateTime? ApprovedAtUtc, DateTime? IssuedAtUtc, string RowVersion, IReadOnlyList<PricedLineResponse> Lines,
    IReadOnlyList<SalesDocumentEventResponse> Events);
