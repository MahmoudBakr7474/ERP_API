using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Reports;

/// <summary>
/// One posting account on a financial statement, with its section and parent for grouping. Amounts carry the section's
/// natural sign: assets and expenses debit minus credit, liabilities, equity and income credit minus debit.
/// </summary>
/// <param name="ComparativeAmount">The comparison period's amount, when one was asked for.</param>
public sealed record StatementLineResponse(
    Guid AccountId, string Code, string NameAr, string? NameEn, StatementSection Section, Guid? ParentId, string? ParentCode,
    string? ParentNameAr, decimal Amount, decimal? ComparativeAmount);
