using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.Users;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.ListUsers;

public sealed class ListUsersHandler(IUserReadStore users, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListUsersQuery, PagedResult<UserSummaryResponse>>
{
    public async Task<Result<PagedResult<UserSummaryResponse>>> HandleAsync(ListUsersQuery query, CancellationToken ct) =>
        await users.ListAsync(Text.Optional(query.Search), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
