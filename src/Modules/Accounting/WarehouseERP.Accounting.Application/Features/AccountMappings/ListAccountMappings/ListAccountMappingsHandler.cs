using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.ListAccountMappings;

// All roles in their order, so the settings screen shows what is still unmapped.
public sealed class ListAccountMappingsHandler(IAccountMappingRepository mappings, IAccountRepository accounts, ICurrentUser currentUser)
    : IQueryHandler<ListAccountMappingsQuery, IReadOnlyList<AccountMappingResponse>>
{
    public async Task<Result<IReadOnlyList<AccountMappingResponse>>> HandleAsync(ListAccountMappingsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var mapped = (await mappings.ListAsync(ct)).ToDictionary(m => m.Role);
        var mappedAccounts = (await accounts.FindManyAsync(mapped.Values.Select(m => m.AccountId).Distinct().ToList(), ct))
            .ToDictionary(a => a.Id);
        var rows = Enum.GetValues<PostingRole>().Select(role =>
        {
            var mapping = mapped.GetValueOrDefault(role);
            var account = mapping is null ? null : mappedAccounts.GetValueOrDefault(mapping.AccountId);
            return new AccountMappingResponse(role, mapping?.AccountId, account?.Code, account?.NameAr, account?.NameEn,
                PostingRoleRules.TypesOf(role), PostingRoleRules.PartyOf(role),
                mapping is null ? null : Convert.ToBase64String(mapping.RowVersion));
        }).ToList();
        return Result.Success<IReadOnlyList<AccountMappingResponse>>(rows);
    }
}
