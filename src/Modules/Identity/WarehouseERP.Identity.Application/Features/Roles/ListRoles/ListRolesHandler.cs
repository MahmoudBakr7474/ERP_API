using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Contracts.Roles;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.ListRoles;

// Roles are few (tens at most), so the list is not paged.
public sealed class ListRolesHandler(IRoleRepository roles) : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<RoleSummaryResponse>>> HandleAsync(ListRolesQuery query, CancellationToken ct) =>
        Result.Success(await roles.ListAsync(ct));
}
