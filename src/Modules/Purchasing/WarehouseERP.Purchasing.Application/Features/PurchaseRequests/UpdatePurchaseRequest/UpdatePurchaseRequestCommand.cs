using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.UpdatePurchaseRequest;

/// <summary>Replaces a draft's date, notes and lines; its warehouse stays.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdatePurchaseRequestCommand(
    Guid PurchaseRequestId, DateOnly? NeededByDate, string? Notes, IReadOnlyList<RequestLineRequest> Lines, string RowVersion);
