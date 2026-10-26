using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Account.GetMe;

// What the React client needs to build its menu: who I am, what I may do, where.
public sealed class GetMeHandler(ICurrentUser currentUser, IUserReadStore users, IEnumerable<PermissionDefinition> catalog)
    : IQueryHandler<GetMeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> HandleAsync(GetMeQuery query, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId || await users.GetAsync(userId, ct) is not { } user)
            return UserErrors.NotFound;

        var permissions = catalog.Select(p => p.Name).Where(currentUser.HasPermission).Order(StringComparer.Ordinal).ToList();
        return new MeResponse(
            user.Id, user.UserName, user.DisplayName, user.Email, user.Roles.Select(r => r.Name).ToList(), permissions,
            currentUser.HasAllWarehouses, currentUser.HasAllWarehouses ? [] : currentUser.WarehouseIds.Order().ToList());
    }
}
