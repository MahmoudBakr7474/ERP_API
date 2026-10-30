namespace WarehouseERP.Purchasing.Contracts;

/// <summary>
/// What a posted supplier return means to the ledger. StockDocumentId: The PurchaseReturn stock document Inventory took
/// the goods out with. ReceivedValue: What its goods returned before invoicing take off goods received not invoiced, at
/// the order's net unit price. ClaimNetAmount: What the supplier owes back for goods returned after invoicing, before
/// tax.
/// </summary>
public sealed record SupplierReturnAccounting(
    Guid Id, string Number, Guid SupplierId, Guid BranchId, DateTime PostedAtUtc, Guid StockDocumentId, decimal ReceivedValue,
    decimal ClaimNetAmount, decimal ClaimTaxAmount, decimal ClaimTotalAmount);
