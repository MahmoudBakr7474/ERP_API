using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateGovernorate;

// Company-wide users only, under an active country. A duplicate Arabic name in the country is refused by the database.
public sealed partial class CreateGovernorateHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<CreateGovernorateHandler> logger)
    : ICommandHandler<CreateGovernorateCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateGovernorateCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var country = await geography.GetCountryByCodeAsync(command.CountryCode.Trim().ToUpperInvariant(), ct);
        if (country is not { IsActive: true })
            return GeographyErrors.CountryNotActive;

        var governorate = Governorate.Create(country.Id, command.NameAr, command.NameEn);
        geography.Add(governorate);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        GovernorateCreated(logger, governorate.Id, country.Code);
        return governorate.Id;
    }

    [LoggerMessage(EventId = 2023, Level = LogLevel.Information, Message = "Governorate {GovernorateId} created in {CountryCode}")]
    private static partial void GovernorateCreated(ILogger logger, Guid governorateId, string countryCode);
}
