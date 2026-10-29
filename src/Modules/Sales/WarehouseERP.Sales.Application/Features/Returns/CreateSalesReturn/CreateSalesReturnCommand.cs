using WarehouseERP.Sales.Application.Contracts.Returns;

namespace WarehouseERP.Sales.Application.Features.Returns.CreateSalesReturn;

/// <summary>A draft return of goods from one posted delivery.</summary>
/// <param name="DeliveryId">A posted delivery in the user's scope.</param>
/// <param name="Reason">Why the customer sends the goods back.</param>
public sealed record CreateSalesReturnCommand(Guid DeliveryId, string Reason, string? Notes, IReadOnlyList<SalesReturnLineRequest> Lines);
