namespace WarehouseERP.Pos.Application.Contracts.Sales;

public sealed record PosSaleSummaryResponse(
    Guid Id, string Number, Guid TerminalId, string TerminalCode, Guid ShiftId, Guid WarehouseId, string WarehouseCode,
    Guid CashierUserId, string? CashierName, Guid? CustomerId, string? CustomerName, DateTime SoldAtUtc, int LineCount,
    decimal TotalAmount, decimal CashReceivedAmount, decimal CardAmount, decimal RefundedAmount, int ReturnCount);
