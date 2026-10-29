namespace WarehouseERP.Pos.Application.Contracts.Returns;

public sealed record PosReturnSummaryResponse(
    Guid Id, string Number, Guid SaleId, string SaleNumber, Guid TerminalId, string TerminalCode, Guid ShiftId, Guid WarehouseId,
    string WarehouseCode, Guid CashierUserId, string? CashierName, string? CustomerName, DateTime ReturnedAtUtc, string Reason,
    decimal TotalAmount, decimal CashRefundAmount, decimal CardRefundAmount);
