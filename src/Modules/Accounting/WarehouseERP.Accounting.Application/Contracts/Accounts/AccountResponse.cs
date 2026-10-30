using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Accounts;

/// <summary>One account with what decides whether it may be changed, deactivated or deleted.</summary>
/// <param name="HasLines">Journals use it, so it can't be deleted.</param>
/// <param name="MappedRoles">The posting roles that use it.</param>
public sealed record AccountResponse(
    Guid Id, string Code, string NameAr, string? NameEn, Guid? ParentId, string? ParentCode, string? ParentNameAr, int Level,
    AccountType Type, StatementSection? Section, bool IsPosting, bool AllowsManualPosting, PartyType? PartyType, bool IsActive,
    bool HasChildren, bool HasLines, IReadOnlyList<PostingRole> MappedRoles, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
    string RowVersion);
