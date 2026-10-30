using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.RemoveAccountMapping;

// An unmapped role stops the postings that need it (they wait as exceptions); nothing falls back to another account.
public sealed partial class RemoveAccountMappingHandler(
    IAccountMappingRepository mappings, ICurrentUser currentUser, ILogger<RemoveAccountMappingHandler> logger)
    : ICommandHandler<RemoveAccountMappingCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(RemoveAccountMappingCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var mapping = await mappings.GetForUpdateAsync(command.Role, expectedRowVersion: null, ct);
        if (mapping is null)
            return MappingErrors.NotMapped;
        mappings.Remove(mapping);
        var saved = await mappings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleUnmapped(logger, command.Role);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9021, Level = LogLevel.Information, Message = "Posting role {Role} unmapped")]
    private static partial void RoleUnmapped(ILogger logger, PostingRole role);
}
