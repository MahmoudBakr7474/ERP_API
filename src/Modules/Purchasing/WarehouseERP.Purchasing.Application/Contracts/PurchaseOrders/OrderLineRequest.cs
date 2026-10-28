using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

/// <summary>One line of a purchase order as the client sends it. Prices are in the company's base currency, before tax.</summary>
/// <param name="ProductId">An active stock item without batch or serial tracking.</param>
/// <param name="ProductUnitId">An active unit of the product; quantities, receipts and invoices are in it.</param>
/// <param name="Quantity">Up to 4 decimals; whole for a unit that can't be split.</param>
/// <param name="UnitPrice">Price of one unit before discount and tax, up to 8 decimals.</param>
/// <param name="DiscountPercent">0 to 100, up to 4 decimals.</param>
/// <param name="TaxRate">Tax percent on the discounted amount, 0 to 100.</param>
/// <param name="TaxTreatment">Only Recoverable is supported; NonRecoverable is refused.</param>
/// <param name="PurchaseRequestLineId">The approved purchase request line this line orders, when any.</param>
public sealed record OrderLineRequest(
    Guid ProductId, Guid ProductUnitId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent = 0, decimal TaxRate = 0,
    TaxTreatment TaxTreatment = TaxTreatment.Recoverable, Guid? PurchaseRequestLineId = null);
