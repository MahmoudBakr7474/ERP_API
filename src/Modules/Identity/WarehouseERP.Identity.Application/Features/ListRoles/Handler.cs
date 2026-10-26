using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.ListRoles;

// Roles are few (tens at most), so the list is not paged.
public sealed class ListRolesHandler(IRoleRepository roles) : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummary>>
{
    public async Task<Result<IReadOnlyList<RoleSummary>>> HandleAsync(ListRolesQuery query, CancellationToken ct) =>
        Result.Success(await roles.ListAsync(ct));
}
