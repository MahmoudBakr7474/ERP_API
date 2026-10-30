namespace WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;

/// <param name="AccountId">A posting account, or a header for all posting accounts below it.</param>
/// <param name="BranchId">Only lines of this branch.</param>
public sealed record GetAccountStatementQuery(Guid AccountId, DateOnly FromDate, DateOnly ToDate, Guid? BranchId);
