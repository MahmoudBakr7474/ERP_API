namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <summary>
/// A POS sale as its receipt prints it: an internal commercial receipt, not a tax invoice (no electronic tax receipt is
/// issued in V1). Amounts are in the base currency; prices are tax-exclusive with the VAT shown per line and per rate.
/// </summary>
/// <param name="IsTaxInvoice">Always false in V1.</param>
/// <param name="IsCopy">True when this is a reprint (or a view of a sale already printed); the receipt says COPY.</param>
/// <param name="CopyNumber">Which copy a reprint is (1 for the first copy).</param>
/// <param name="TimeZoneId">The company's time zone, to print local dates and times.</param>
public sealed record PosSaleResponse(
    Guid Id, string Number, bool IsTaxInvoice, bool IsCopy, int? CopyNumber, int PrintCount, string CompanyName, string TimeZoneId,
    Guid BranchId, string BranchCode, string BranchName, Guid WarehouseId, string WarehouseCode, Guid TerminalId, string TerminalCode,
    string TerminalName, string? ReceiptHeader, string? ReceiptFooter, Guid ShiftId, string ShiftNumber, Guid CashierUserId,
    string? CashierName, Guid? CustomerId, string? CustomerName, string? CustomerPhone, string? CustomerTaxNumber,
    string CurrencyCode, int CurrencyDecimals, DateTime SoldAtUtc, IReadOnlyList<PosSaleLineResponse> Lines,
    IReadOnlyList<PosTaxLineResponse> TaxBreakdown, decimal GrossAmount, decimal DiscountAmount, decimal NetAmount,
    decimal TaxAmount, decimal TotalAmount, IReadOnlyList<PosPaymentResponse> Payments, decimal CashTenderedAmount,
    decimal ChangeAmount, decimal CashReceivedAmount, decimal CardAmount, decimal RefundedAmount,
    IReadOnlyList<PosReturnReferenceResponse> Returns, Guid? StockDocumentId, string? StockDocumentNumber, string? Notes);
