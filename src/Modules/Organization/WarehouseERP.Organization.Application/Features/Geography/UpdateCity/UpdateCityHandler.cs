using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCity;

// Company-wide users only; the governorate is fixed. Returns the new row version for the caller's next update.
public sealed partial class UpdateCityHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<UpdateCityHandler> logger)
    : ICommandHandler<UpdateCityCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCityCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var city = await geography.GetCityAsync(command.CityId, expected, ct);
        if (city is null)
            return GeographyErrors.CityNotFound;

        city.Rename(command.NameAr, command.NameEn);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CityRenamed(logger, city.Id);
        return Convert.ToBase64String(city.RowVersion);
    }

    [LoggerMessage(EventId = 2027, Level = LogLevel.Information, Message = "City {CityId} renamed")]
    private static partial void CityRenamed(ILogger logger, Guid cityId);
}
