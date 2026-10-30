using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.SetAccountMapping;

// Maps a role to an active posting account that fits it (type and party). Changing a mapping affects only journals the
// system writes from now on; posted journals keep their accounts. Returns the mapping's new row version.
public sealed partial class SetAccountMappingHandler(
    IAccountMappingRepository mappings, IAccountRepository accounts, ICurrentUser currentUser, ILogger<SetAccountMappingHandler> logger)
    : ICommandHandler<SetAccountMappingCommand, string>
{
    public async Task<Result<string>> HandleAsync(SetAccountMappingCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var account = await accounts.FindAsync(command.AccountId, ct);
        if (account is null)
            return MappingErrors.AccountNotFound;

        var existing = await mappings.FindAsync(command.Role, ct);
        AccountMapping mapping;
        if (existing is null)
        {
            var created = AccountMapping.Create(command.Role, account);
            if (created.IsFailure)
                return created.Error;
            mapping = created.Value;
            mappings.Add(mapping);
        }
        else
        {
            if (!RowVersions.TryParse(command.RowVersion, out var expected))
                return RowVersionErrors.Invalid;
            var tracked = await mappings.GetForUpdateAsync(command.Role, expected, ct);
            if (tracked is null)
                return CommonErrors.ConcurrencyConflict; // removed meanwhile
            mapping = tracked;
            var remapped = mapping.Remap(account);
            if (remapped.IsFailure)
                return remapped.Error;
        }
        var saved = await mappings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleMapped(logger, command.Role, account.Code);
        return Convert.ToBase64String(mapping.RowVersion);
    }

    [LoggerMessage(EventId = 9020, Level = LogLevel.Information, Message = "Posting role {Role} mapped to account {Code}")]
    private static partial void RoleMapped(ILogger logger, PostingRole role, string code);
}
