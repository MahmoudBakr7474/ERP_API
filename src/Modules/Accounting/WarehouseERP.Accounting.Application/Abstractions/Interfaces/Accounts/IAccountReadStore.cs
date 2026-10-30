using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;

/// <summary>Read side of the chart of accounts.</summary>
public interface IAccountReadStore
{
    Task<AccountResponse?> GetAsync(Guid accountId, CancellationToken ct);

    /// <summary>A page of accounts in code order.</summary>
    Task<PagedResult<AccountSummaryResponse>> ListAsync(AccountFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>The whole chart in code order (reports and the tree view).</summary>
    Task<IReadOnlyList<AccountSummaryResponse>> ListAllAsync(CancellationToken ct);
}
