using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ListAccounts;

/// <param name="Search">Part of the code or of either name.</param>
/// <param name="ParentId">Only the direct children of this account.</param>
public sealed record ListAccountsQuery(
    int Page, int? PageSize, string? Search, AccountType? Type, bool? IsActive, bool? IsPosting, Guid? ParentId) : IPagedQuery;
