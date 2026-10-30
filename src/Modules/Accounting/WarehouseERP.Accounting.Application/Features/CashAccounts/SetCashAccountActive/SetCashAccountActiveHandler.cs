using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.SetCashAccountActive;

// An inactive cashbox or bank takes no new vouchers; it is deactivated only once it holds no money.
public sealed partial class SetCashAccountActiveHandler(
    ICashAccountRepository cashAccounts, IAccountRepository accounts, ICurrentUser currentUser, ILogger<SetCashAccountActiveHandler> logger)
    : ICommandHandler<SetCashAccountActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetCashAccountActiveCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var cash = await cashAccounts.GetForUpdateAsync(command.CashAccountId, expectedRowVersion: null, ct);
        if (cash is null)
            return CashAccountErrors.NotFound;
        if (cash.IsActive == command.IsActive)
            return Done.Value;
        if (command.IsActive)
            cash.Activate();
        else
        {
            if (await accounts.PostedBalanceAsync(cash.AccountId, ct) != 0m)
                return CashAccountErrors.HasBalance;
            cash.Deactivate();
        }
        var saved = await cashAccounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ActiveChanged(logger, cash.Id, cash.Code, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9072, Level = LogLevel.Information, Message = "Cash account {CashAccountId} ({Code}) active: {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid cashAccountId, string code, bool isActive);
}
