using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.SetCountryActive;

// Company-wide users only. Deactivating a country leaves its governorates and cities as they are: none of them is
// offered for new addresses while the country is inactive, and addresses that already name them keep them.
public sealed partial class SetCountryActiveHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<SetCountryActiveHandler> logger)
    : ICommandHandler<SetCountryActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetCountryActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var country = await geography.GetCountryAsync(command.CountryId, expectedRowVersion: null, ct);
        if (country is null)
            return GeographyErrors.CountryNotFound;

        if (command.IsActive) country.Activate();
        else country.Deactivate();
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, country.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2022, Level = LogLevel.Information, Message = "Country {CountryId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid countryId, bool isActive);
}
