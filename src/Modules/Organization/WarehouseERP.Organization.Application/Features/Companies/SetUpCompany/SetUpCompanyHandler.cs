using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;

// First run: one company per installation (V1), set up by a company-wide user. The repository refuses a second
// company even when two setups race.
public sealed partial class SetUpCompanyHandler(
    ICompanyRepository companies, ICurrentUser currentUser, ILogger<SetUpCompanyHandler> logger)
    : ICommandHandler<SetUpCompanyCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(SetUpCompanyCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;

        var company = Company.Create(
            command.Code, command.NameAr, command.NameEn, command.BaseCurrencyCode, command.TimeZoneId,
            command.TaxRegistrationNumber);
        var saved = await companies.AddIfNoneAsync(company, ct);
        if (saved.IsFailure)
            return saved.Error;

        CompanySetUp(logger, company.Id, company.Code, company.BaseCurrencyCode);
        return company.Id;
    }

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Company {CompanyId} ({CompanyCode}) set up with base currency {BaseCurrencyCode}")]
    private static partial void CompanySetUp(ILogger logger, Guid companyId, string companyCode, string baseCurrencyCode);
}
