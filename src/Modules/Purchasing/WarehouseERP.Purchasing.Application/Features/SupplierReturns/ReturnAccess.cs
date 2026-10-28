using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns;

// A return belongs to its receipt's warehouse scope. The endpoint already checked the permission of the action.
internal static class ReturnAccess
{
    public static async Task<Result> AuthorizeAsync(ISupplierReturnRepository returns, ICurrentUser user, Guid returnId, CancellationToken ct) =>
        DocumentAccess.Check(user, await returns.FindWarehouseAsync(returnId, ct), SupplierReturnErrors.NotFound);
}
