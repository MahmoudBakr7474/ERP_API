namespace WarehouseERP.Pos.Contracts;

/// <summary>
/// What a POS return means to the ledger. StockDocumentId: The PosReturn stock document Inventory took restocked goods
/// back with, if any.
/// </summary>
public sealed record PosReturnAccounting(
    Guid Id, string Number, string SaleNumber, Guid BranchId, Guid ShiftId, DateTime ReturnedAtUtc, Guid? StockDocumentId,
    decimal NetAmount, decimal TaxAmount, decimal CashRefundAmount, decimal CardRefundAmount);
