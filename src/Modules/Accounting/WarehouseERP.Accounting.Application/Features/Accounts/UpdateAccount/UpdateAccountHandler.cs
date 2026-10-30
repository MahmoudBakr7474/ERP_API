using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.UpdateAccount;

// From the version the editor read. A new section moves the account to another line of the statements, for every period
// (statements are always computed from the ledger); posted journals don't change.
public sealed partial class UpdateAccountHandler(IAccountRepository accounts, ICurrentUser currentUser, ILogger<UpdateAccountHandler> logger)
    : ICommandHandler<UpdateAccountCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateAccountCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var account = await accounts.GetForUpdateAsync(command.AccountId, expected, ct);
        if (account is null)
            return AccountErrors.NotFound;
        var updated = account.Update(command.NameAr, command.NameEn, command.Section, command.AllowsManualPosting);
        if (updated.IsFailure)
            return updated.Error;
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AccountUpdated(logger, account.Id, account.Code);
        return Convert.ToBase64String(account.RowVersion);
    }

    [LoggerMessage(EventId = 9011, Level = LogLevel.Information, Message = "Account {AccountId} ({Code}) updated")]
    private static partial void AccountUpdated(ILogger logger, Guid accountId, string code);
}
