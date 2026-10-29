using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Application.Contracts.Returns;

/// <summary>A POS return as its receipt prints it, with the sale it came back against.</summary>
/// <param name="IsCopy">True when this is a reprint (or a view of a return already printed).</param>
/// <param name="CopyNumber">Which copy a reprint is (1 for the first copy).</param>
/// <param name="TaxBreakdown">Net and VAT refunded per treatment and rate.</param>
public sealed record PosReturnResponse(
    Guid Id, string Number, bool IsCopy, int? CopyNumber, int PrintCount, string CompanyName, string TimeZoneId, Guid BranchId,
    string BranchCode, string BranchName, Guid WarehouseId, string WarehouseCode, Guid TerminalId, string TerminalCode,
    string TerminalName, string? ReceiptHeader, string? ReceiptFooter, Guid ShiftId, string ShiftNumber, Guid CashierUserId,
    string? CashierName, Guid SaleId, string SaleNumber, DateTime SoldAtUtc, Guid? CustomerId, string? CustomerName,
    string Reason, string? Notes, string CurrencyCode, int CurrencyDecimals, DateTime ReturnedAtUtc,
    IReadOnlyList<PosReturnLineResponse> Lines, IReadOnlyList<PosTaxLineResponse> TaxBreakdown, decimal NetAmount, decimal TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<PosRefundResponse> Refunds, decimal CashRefundAmount, decimal CardRefundAmount, Guid? StockDocumentId,
    string? StockDocumentNumber);
