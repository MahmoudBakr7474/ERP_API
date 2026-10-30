namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// What an approved customer return means to the ledger. StockDocumentId: The SalesReturn stock document Inventory took
/// the goods back with (their cost). UnbilledAmount: What its goods returned before invoicing take off the unbilled
/// value of their order lines. CreditNetAmount: The credit owed to the customer for goods returned after invoicing,
/// before VAT.
/// </summary>
public sealed record SalesReturnAccounting(
    Guid Id, string Number, Guid CustomerId, Guid BranchId, DateTime ApprovedAtUtc, Guid StockDocumentId, decimal UnbilledAmount,
    decimal CreditNetAmount, decimal CreditTaxAmount, decimal CreditTotalAmount);
