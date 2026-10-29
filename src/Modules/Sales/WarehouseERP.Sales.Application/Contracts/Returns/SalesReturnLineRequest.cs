using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Returns;

/// <summary>One returned quantity as the client sends it.</summary>
/// <param name="DeliveryLineId">A line of the return's delivery.</param>
/// <param name="Quantity">In the delivery line's unit; at most what it delivered and has not had back yet.</param>
/// <param name="Condition">Only FitForResale is accepted in V1; Damaged and Uninspected are refused.</param>
/// <param name="LocationId">A location of the delivery's warehouse to restock into; its DEFAULT location when left out.</param>
public sealed record SalesReturnLineRequest(
    Guid DeliveryLineId, decimal Quantity, ReturnCondition Condition = ReturnCondition.FitForResale, Guid? LocationId = null);
