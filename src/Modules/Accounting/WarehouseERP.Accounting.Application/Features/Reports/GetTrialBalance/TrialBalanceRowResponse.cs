using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;

/// <summary>One account: its opening and closing balance on one side each, and the period's debits and credits.</summary>
public sealed record TrialBalanceRowResponse(
    Guid AccountId, string Code, string NameAr, string? NameEn, Guid? ParentId, int Level, AccountType Type, bool IsPosting,
    decimal OpeningDebit, decimal OpeningCredit, decimal PeriodDebit, decimal PeriodCredit, decimal ClosingDebit, decimal ClosingCredit);
