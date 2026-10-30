using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.ListAccountMappings;

/// <param name="AccountId">Null while the role is not mapped: postings that need it stop until it is.</param>
/// <param name="AllowedTypes">The account types the role accepts.</param>
/// <param name="RequiredParty">The party its account's lines must name, if any.</param>
/// <param name="RowVersion">Sent back to change an existing mapping; null while unmapped.</param>
public sealed record AccountMappingResponse(
    PostingRole Role, Guid? AccountId, string? AccountCode, string? AccountNameAr, string? AccountNameEn,
    IReadOnlyList<AccountType> AllowedTypes, PartyType? RequiredParty, string? RowVersion);
