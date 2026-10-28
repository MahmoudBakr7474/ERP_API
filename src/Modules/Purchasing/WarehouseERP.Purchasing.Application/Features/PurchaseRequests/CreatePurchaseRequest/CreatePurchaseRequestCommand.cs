using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CreatePurchaseRequest;

/// <param name="WarehouseId">An active physical warehouse in the user's scope; orders drawing on the request are for it.</param>
public sealed record CreatePurchaseRequestCommand(Guid WarehouseId, DateOnly? NeededByDate, string? Notes, IReadOnlyList<RequestLineRequest> Lines);
