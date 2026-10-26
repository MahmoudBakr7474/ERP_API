using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Identity.Application.Features.Users.ListUsers;

/// <param name="Search">Matches user name, display name or email.</param>
public sealed record ListUsersQuery(int Page, int? PageSize, string? Search) : IPagedQuery;
