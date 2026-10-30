using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;

/// <param name="AccountId">A posting account, or a header for all posting accounts below it; null for every account.</param>
public sealed record ListGeneralLedgerQuery(
    int Page, int? PageSize, DateOnly FromDate, DateOnly ToDate, Guid? AccountId, Guid? BranchId, JournalType? JournalType) : IPagedQuery;
