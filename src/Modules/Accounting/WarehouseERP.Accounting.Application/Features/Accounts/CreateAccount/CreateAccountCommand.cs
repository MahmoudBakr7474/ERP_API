using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Accounts.CreateAccount;

/// <param name="ParentId">A header account the new one belongs to; null for a top-level class.</param>
/// <param name="Section">Required for a posting account.</param>
/// <param name="AllowsManualPosting">Ignored for a header account.</param>
/// <param name="PartyType">Makes a posting account a customer or supplier control account.</param>
public sealed record CreateAccountCommand(
    string Code, string NameAr, string? NameEn, Guid? ParentId, AccountType Type, StatementSection? Section, bool IsPosting,
    bool AllowsManualPosting, PartyType? PartyType);
