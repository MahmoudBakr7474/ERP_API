using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCountry;

// Company-wide users only; the code is fixed. Returns the new row version for the caller's next update.
public sealed partial class UpdateCountryHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<UpdateCountryHandler> logger)
    : ICommandHandler<UpdateCountryCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCountryCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var country = await geography.GetCountryAsync(command.CountryId, expected, ct);
        if (country is null)
            return GeographyErrors.CountryNotFound;

        country.Rename(command.NameAr, command.NameEn);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountryRenamed(logger, country.Id);
        return Convert.ToBase64String(country.RowVersion);
    }

    [LoggerMessage(EventId = 2021, Level = LogLevel.Information, Message = "Country {CountryId} renamed")]
    private static partial void CountryRenamed(ILogger logger, Guid countryId);
}
