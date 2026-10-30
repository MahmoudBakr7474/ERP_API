using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.CreateCashAccount;

// A cashbox or bank account of an active branch, kept on its own active asset posting account (unique indexes decide when
// two users take one code or one ledger account at once).
public sealed partial class CreateCashAccountHandler(
    ICashAccountRepository cashAccounts, IAccountRepository accounts, AccountingReferences references, ICurrentUser currentUser,
    ILogger<CreateCashAccountHandler> logger)
    : ICommandHandler<CreateCashAccountCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCashAccountCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (await cashAccounts.CodeExistsAsync(command.Code.Trim().ToUpperInvariant(), ct))
            return CashAccountErrors.CodeTaken;
        var account = await accounts.FindAsync(command.AccountId, ct);
        if (account is null)
            return CashAccountErrors.AccountNotFound;
        if (await cashAccounts.LedgerAccountUsedAsync(account.Id, ct))
            return CashAccountErrors.AccountInUse;
        var branch = (await references.BranchesAsync([command.BranchId], ct)).GetValueOrDefault(command.BranchId);
        if (branch is null)
            return CashAccountErrors.BranchNotFound;
        if (!branch.IsActive)
            return CashAccountErrors.BranchNotActive;
        if (command.ReceivesPosCash && await cashAccounts.PosCashboxTakenAsync(branch.Id, null, ct))
            return CashAccountErrors.PosCashboxTaken;
        var created = CashAccount.Create(command.Code, command.Name, command.Kind, branch.Id, account, command.BankName, command.BankAccountNumber,
            command.AllowsOverdraft, command.ReceivesPosCash);
        if (created.IsFailure)
            return created.Error;
        cashAccounts.Add(created.Value);
        var saved = await cashAccounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        CashAccountCreated(logger, created.Value.Id, created.Value.Code, created.Value.Kind);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9070, Level = LogLevel.Information, Message = "Cash account {CashAccountId} ({Code}, {Kind}) created")]
    private static partial void CashAccountCreated(ILogger logger, Guid cashAccountId, string code, CashAccountKind kind);
}
