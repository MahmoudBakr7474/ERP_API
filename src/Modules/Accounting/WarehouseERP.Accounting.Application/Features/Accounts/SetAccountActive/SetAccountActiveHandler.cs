using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.SetAccountActive;

// An inactive account takes no new lines. Deactivating waits until its children are inactive, no posting role uses it and
// its posted balance is zero; activating needs an active parent.
public sealed partial class SetAccountActiveHandler(IAccountRepository accounts, ICurrentUser currentUser, ILogger<SetAccountActiveHandler> logger)
    : ICommandHandler<SetAccountActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetAccountActiveCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var account = await accounts.GetForUpdateAsync(command.AccountId, expectedRowVersion: null, ct);
        if (account is null)
            return AccountErrors.NotFound;
        if (account.IsActive == command.IsActive)
            return Done.Value;

        if (command.IsActive)
        {
            if (account.ParentId is { } parentId && await accounts.FindAsync(parentId, ct) is { IsActive: false })
                return AccountDomainErrors.ParentNotActive;
            account.Activate();
        }
        else
        {
            if (await accounts.HasChildrenAsync(account.Id, activeOnly: true, ct))
                return AccountErrors.HasActiveChildren;
            if (await accounts.IsMappedAsync(account.Id, ct))
                return AccountErrors.Mapped;
            if (await accounts.PostedBalanceAsync(account.Id, ct) != 0m)
                return AccountErrors.HasBalance;
            account.Deactivate();
        }
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ActiveChanged(logger, account.Id, account.Code, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9012, Level = LogLevel.Information, Message = "Account {AccountId} ({Code}) active: {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid accountId, string code, bool isActive);
}
