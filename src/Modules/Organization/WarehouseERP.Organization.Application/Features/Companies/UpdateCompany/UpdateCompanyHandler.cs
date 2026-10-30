using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;

// Company-wide users only. Returns the new row version for the caller's next update. A new city must be usable; the
// current one may stay after it was retired, so an unrelated change never fails on it.
public sealed partial class UpdateCompanyHandler(
    ICompanyRepository companies, IGeographyDirectory geography, ICurrentUser currentUser, ILogger<UpdateCompanyHandler> logger)
    : ICommandHandler<UpdateCompanyCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCompanyCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var company = await companies.GetForUpdateAsync(expected, ct);
        if (company is null)
            return CompanyErrors.NotConfigured;

        if (command.CityId is { } cityId && cityId != company.CityId && !await CompanyCity.IsUsableAsync(geography, cityId, ct))
            return CompanyErrors.CityNotUsable;

        company.UpdateSettings(command.NameAr, command.NameEn, command.TimeZoneId, command.TaxRegistrationNumber);
        company.SetContactDetails(new CompanyContactDetails(
            command.CommercialRegistrationNumber, command.CommercialRegistry, command.Phone, command.Email, command.AddressLine,
            command.CityId));
        var saved = await companies.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CompanyUpdated(logger, company.Id);
        return Convert.ToBase64String(company.RowVersion);
    }

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Company {CompanyId} settings updated")]
    private static partial void CompanyUpdated(ILogger logger, Guid companyId);
}
