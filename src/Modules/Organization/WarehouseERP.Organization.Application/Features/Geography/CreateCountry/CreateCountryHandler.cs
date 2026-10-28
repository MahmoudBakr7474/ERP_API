using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateCountry;

// Company-wide users only. The pre-check gives a clear error; the database still guards the code and name on a race.
public sealed partial class CreateCountryHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<CreateCountryHandler> logger)
    : ICommandHandler<CreateCountryCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCountryCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var country = Country.Create(command.Code, command.NameAr, command.NameEn);
        if (await geography.GetCountryByCodeAsync(country.Code, ct) is not null)
            return GeographyErrors.CountryCodeTaken;

        geography.Add(country);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountryCreated(logger, country.Id, country.Code);
        return country.Id;
    }

    [LoggerMessage(EventId = 2020, Level = LogLevel.Information, Message = "Country {CountryId} ({CountryCode}) created")]
    private static partial void CountryCreated(ILogger logger, Guid countryId, string countryCode);
}
