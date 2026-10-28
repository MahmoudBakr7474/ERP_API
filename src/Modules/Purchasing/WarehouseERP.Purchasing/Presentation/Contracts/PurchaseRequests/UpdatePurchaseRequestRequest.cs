using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

namespace WarehouseERP.Purchasing.Presentation.Contracts.PurchaseRequests;

public sealed record UpdatePurchaseRequestRequest(DateOnly? NeededByDate, string? Notes, IReadOnlyList<RequestLineRequest> Lines, string RowVersion);
