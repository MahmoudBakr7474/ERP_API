using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.DeleteAccount;

// While the chart is being prepared, an account nothing uses may go: no children, no journal line (posted or not) and no
// posting role. Once used it is deactivated instead; the temporal history keeps the deleted row.
public sealed partial class DeleteAccountHandler(IAccountRepository accounts, ICurrentUser currentUser, ILogger<DeleteAccountHandler> logger)
    : ICommandHandler<DeleteAccountCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(DeleteAccountCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var account = await accounts.GetForUpdateAsync(command.AccountId, expectedRowVersion: null, ct);
        if (account is null)
            return AccountErrors.NotFound;
        if (await accounts.HasChildrenAsync(account.Id, activeOnly: false, ct))
            return AccountErrors.HasChildren;
        if (await accounts.HasLinesAsync(account.Id, ct))
            return AccountErrors.HasLines;
        if (await accounts.IsMappedAsync(account.Id, ct))
            return AccountErrors.Mapped;
        accounts.Remove(account);
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AccountDeleted(logger, account.Id, account.Code);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9013, Level = LogLevel.Information, Message = "Account {AccountId} ({Code}) deleted")]
    private static partial void AccountDeleted(ILogger logger, Guid accountId, string code);
}
