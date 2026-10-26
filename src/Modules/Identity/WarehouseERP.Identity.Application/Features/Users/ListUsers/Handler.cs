using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.ListUsers;

public sealed class ListUsersHandler(IUserReadStore users, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListUsersQuery, PagedResult<UserSummary>>
{
    public async Task<Result<PagedResult<UserSummary>>> HandleAsync(ListUsersQuery query, CancellationToken ct) =>
        await users.ListAsync(Text.Optional(query.Search), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
