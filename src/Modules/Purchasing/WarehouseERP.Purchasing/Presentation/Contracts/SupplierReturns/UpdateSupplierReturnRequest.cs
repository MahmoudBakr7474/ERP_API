using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

namespace WarehouseERP.Purchasing.Presentation.Contracts.SupplierReturns;

public sealed record UpdateSupplierReturnRequest(string Reason, string? Notes, IReadOnlyList<ReturnLineRequest> Lines, string RowVersion);
