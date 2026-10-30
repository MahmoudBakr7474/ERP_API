namespace WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;

/// <summary>An account's posted lines over a date range with running balances (debit minus credit).</summary>
/// <param name="IncludesChildren">True for a header account: its posting accounts' lines together.</param>
public sealed record AccountStatementResponse(
    Guid AccountId, string Code, string NameAr, string? NameEn, bool IncludesChildren, DateOnly FromDate, DateOnly ToDate, Guid? BranchId,
    string CurrencyCode, decimal OpeningBalance, decimal TotalDebit, decimal TotalCredit, decimal ClosingBalance,
    IReadOnlyList<AccountStatementLineResponse> Lines);
