namespace WarehouseERP.Purchasing.Domain;

/// <summary>An order line as entered, with its product unit's facts from Catalog.</summary>
/// <param name="Quantity">Quantity in the product unit.</param>
/// <param name="PurchaseRequestLineId">The approved request line it orders, if any; the same product unit.</param>
public sealed record OrderLineInput(ProductUnitFacts Unit, decimal Quantity, LinePrice Price, Guid? PurchaseRequestLineId);
