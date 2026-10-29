using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One returned quantity of a delivery line, in its unit, the condition it came back in and where it goes.</summary>
/// <param name="Unit">The delivery line's product unit, with Catalog's fraction rules for checking the quantity.</param>
/// <param name="LocationId">A location of the delivery's warehouse it goes back into.</param>
public sealed record ReturnLineInput(Guid DeliveryLineId, ProductUnitFacts Unit, decimal Quantity, ReturnCondition Condition, Guid LocationId);
