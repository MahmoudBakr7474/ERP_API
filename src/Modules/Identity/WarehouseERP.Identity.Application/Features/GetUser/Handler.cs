using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.GetUser;

public sealed class GetUserHandler(IUserReadStore users) : IQueryHandler<GetUserQuery, UserDetails>
{
    public async Task<Result<UserDetails>> HandleAsync(GetUserQuery query, CancellationToken ct) =>
        await users.GetAsync(query.UserId, ct) is { } user ? user : IdentityApplicationErrors.UserNotFound;
}
