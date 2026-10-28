using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateCity;

// Company-wide users only, under an active governorate. A duplicate Arabic name in the governorate is refused by the
// database.
public sealed partial class CreateCityHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<CreateCityHandler> logger)
    : ICommandHandler<CreateCityCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCityCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var governorate = await geography.GetGovernorateAsync(command.GovernorateId, expectedRowVersion: null, ct);
        if (governorate is not { IsActive: true })
            return GeographyErrors.GovernorateNotActive;

        var city = City.Create(governorate.Id, command.NameAr, command.NameEn);
        geography.Add(city);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CityCreated(logger, city.Id, governorate.Id);
        return city.Id;
    }

    [LoggerMessage(EventId = 2026, Level = LogLevel.Information, Message = "City {CityId} created in governorate {GovernorateId}")]
    private static partial void CityCreated(ILogger logger, Guid cityId, Guid governorateId);
}
