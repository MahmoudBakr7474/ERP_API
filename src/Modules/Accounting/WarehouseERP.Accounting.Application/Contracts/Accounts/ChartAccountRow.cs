using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Accounts;

/// <summary>One account of a chart being imported (or of the default chart); its parent is named by code.</summary>
public sealed record ChartAccountRow(
    string Code, string NameAr, string? NameEn, string? ParentCode, AccountType Type, StatementSection? Section, bool IsPosting,
    bool AllowsManualPosting, PartyType? PartyType);
