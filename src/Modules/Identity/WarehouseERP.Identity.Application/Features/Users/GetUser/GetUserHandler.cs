using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.GetUser;

public sealed class GetUserHandler(IUserReadStore users) : IQueryHandler<GetUserQuery, UserDetailsResponse>
{
    public async Task<Result<UserDetailsResponse>> HandleAsync(GetUserQuery query, CancellationToken ct) =>
        await users.GetAsync(query.UserId, ct) is { } user ? user : UserErrors.NotFound;
}
