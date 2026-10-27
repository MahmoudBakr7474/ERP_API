using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;

// Company-wide users only. Returns the new row version for the caller's next update.
public sealed partial class UpdateCompanyHandler(
    ICompanyRepository companies, ICurrentUser currentUser, ILogger<UpdateCompanyHandler> logger)
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

        company.UpdateSettings(command.NameAr, command.NameEn, command.TimeZoneId, command.TaxRegistrationNumber);
        var saved = await companies.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CompanyUpdated(logger, company.Id);
        return Convert.ToBase64String(company.RowVersion);
    }

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Company {CompanyId} settings updated")]
    private static partial void CompanyUpdated(ILogger logger, Guid companyId);
}
