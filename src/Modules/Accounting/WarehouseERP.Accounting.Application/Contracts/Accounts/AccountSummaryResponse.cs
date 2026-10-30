using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Accounts;

/// <summary>An account as lists, pickers and the chart tree show it.</summary>
public sealed record AccountSummaryResponse(
    Guid Id, string Code, string NameAr, string? NameEn, Guid? ParentId, int Level, AccountType Type, StatementSection? Section,
    bool IsPosting, bool AllowsManualPosting, PartyType? PartyType, bool IsActive);
