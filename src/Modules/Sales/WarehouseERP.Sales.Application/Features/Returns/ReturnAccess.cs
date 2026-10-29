using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns;

// A customer return belongs to its warehouse's scope (its delivery's). The endpoint already checked the permission of the action.
internal static class ReturnAccess
{
    public static async Task<Result> AuthorizeAsync(ISalesReturnRepository returns, ICurrentUser user, Guid returnId, CancellationToken ct) =>
        DocumentAccess.Check(user, await returns.FindWarehouseAsync(returnId, ct), ReturnErrors.NotFound);
}
