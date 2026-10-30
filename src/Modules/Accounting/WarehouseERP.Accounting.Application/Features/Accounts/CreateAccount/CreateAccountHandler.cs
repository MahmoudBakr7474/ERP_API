using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.CreateAccount;

// A new account under an active header account (or a new top-level class). Its code must extend its parent's and be free;
// the unique index decides when two users take one code at once.
public sealed partial class CreateAccountHandler(IAccountRepository accounts, ICurrentUser currentUser, ILogger<CreateAccountHandler> logger)
    : ICommandHandler<CreateAccountCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateAccountCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var code = command.Code.Trim();
        if (await accounts.CodeExistsAsync(code, ct))
            return AccountErrors.CodeTaken;
        Account? parent = null;
        if (command.ParentId is { } parentId && (parent = await accounts.FindAsync(parentId, ct)) is null)
            return AccountErrors.ParentNotFound;
        var created = Account.Create(code, command.NameAr, command.NameEn, parent, command.Type, command.Section, command.IsPosting,
            command.AllowsManualPosting, command.PartyType);
        if (created.IsFailure)
            return created.Error;
        accounts.Add(created.Value);
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AccountCreated(logger, created.Value.Id, created.Value.Code);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9010, Level = LogLevel.Information, Message = "Account {AccountId} ({Code}) created")]
    private static partial void AccountCreated(ILogger logger, Guid accountId, string code);
}
