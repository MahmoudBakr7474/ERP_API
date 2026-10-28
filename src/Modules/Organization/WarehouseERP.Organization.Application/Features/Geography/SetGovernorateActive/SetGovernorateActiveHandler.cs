using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.SetGovernorateActive;

// Company-wide users only. Its cities keep their own flags; none is offered for new addresses while it is inactive.
public sealed partial class SetGovernorateActiveHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<SetGovernorateActiveHandler> logger)
    : ICommandHandler<SetGovernorateActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetGovernorateActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var governorate = await geography.GetGovernorateAsync(command.GovernorateId, expectedRowVersion: null, ct);
        if (governorate is null)
            return GeographyErrors.GovernorateNotFound;

        if (command.IsActive) governorate.Activate();
        else governorate.Deactivate();
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, governorate.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2025, Level = LogLevel.Information, Message = "Governorate {GovernorateId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid governorateId, bool isActive);
}
