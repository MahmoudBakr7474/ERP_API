using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.ListPermissions;

// The catalog every module registered at startup: what a role can be given.
public sealed class ListPermissionsHandler(IEnumerable<PermissionDefinition> catalog)
    : IQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionDefinition>>
{
    public Task<Result<IReadOnlyList<PermissionDefinition>>> HandleAsync(ListPermissionsQuery query, CancellationToken ct) =>
        Task.FromResult(Result.Success<IReadOnlyList<PermissionDefinition>>(
            catalog.OrderBy(p => p.Module, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal).ToList()));
}
