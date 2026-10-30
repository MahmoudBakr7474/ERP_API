using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Presentation.Contracts.Accounts;

/// <summary>What may change on an account; its code, parent, type, kind and party type are fixed.</summary>
public sealed record UpdateAccountRequest(string NameAr, string? NameEn, StatementSection? Section, bool AllowsManualPosting, string RowVersion);
