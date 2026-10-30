using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.UpdateCashAccount;

// From the version the editor read. Withdrawing an overdraft applies to the next posting; nothing posted changes.
public sealed partial class UpdateCashAccountHandler(
    ICashAccountRepository cashAccounts, ICurrentUser currentUser, ILogger<UpdateCashAccountHandler> logger)
    : ICommandHandler<UpdateCashAccountCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCashAccountCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var cash = await cashAccounts.GetForUpdateAsync(command.CashAccountId, expected, ct);
        if (cash is null)
            return CashAccountErrors.NotFound;
        var updated = cash.Update(command.Name, command.BankName, command.BankAccountNumber, command.AllowsOverdraft);
        if (updated.IsFailure)
            return updated.Error;
        var saved = await cashAccounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        CashAccountUpdated(logger, cash.Id, cash.Code);
        return Convert.ToBase64String(cash.RowVersion);
    }

    [LoggerMessage(EventId = 9071, Level = LogLevel.Information, Message = "Cash account {CashAccountId} ({Code}) updated")]
    private static partial void CashAccountUpdated(ILogger logger, Guid cashAccountId, string code);
}
