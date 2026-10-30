using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Accounts.UpdateAccount;

/// <summary>What may change on an account; its code, parent, type, kind and party type are fixed.</summary>
public sealed record UpdateAccountCommand(
    Guid AccountId, string NameAr, string? NameEn, StatementSection? Section, bool AllowsManualPosting, string RowVersion);
