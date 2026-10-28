using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.SetCityActive;

// Company-wide users only. An inactive city is not offered for new addresses; addresses that name it keep it.
public sealed partial class SetCityActiveHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<SetCityActiveHandler> logger)
    : ICommandHandler<SetCityActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetCityActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var city = await geography.GetCityAsync(command.CityId, expectedRowVersion: null, ct);
        if (city is null)
            return GeographyErrors.CityNotFound;

        if (command.IsActive) city.Activate();
        else city.Deactivate();
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, city.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2028, Level = LogLevel.Information, Message = "City {CityId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid cityId, bool isActive);
}
