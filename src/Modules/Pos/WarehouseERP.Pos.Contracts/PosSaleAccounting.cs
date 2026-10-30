namespace WarehouseERP.Pos.Contracts;

/// <summary>
/// What a POS sale means to the ledger. StockDocumentId: The PosSale stock document Inventory took the goods out with
/// (their cost). CashAmount: Cash kept in the drawer: tendered less change. CardAmount: Charged on the card terminal;
/// settled by the acquirer outside POS.
/// </summary>
public sealed record PosSaleAccounting(
    Guid Id, string Number, Guid BranchId, Guid ShiftId, DateTime SoldAtUtc, Guid? StockDocumentId, decimal NetAmount,
    decimal TaxAmount, decimal CashAmount, decimal CardAmount);
