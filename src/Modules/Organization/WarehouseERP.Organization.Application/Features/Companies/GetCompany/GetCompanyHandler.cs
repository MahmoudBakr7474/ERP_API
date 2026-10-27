using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Contracts.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Companies.GetCompany;

// Not found until the company is set up: the client shows its setup screen then.
public sealed class GetCompanyHandler(ICompanyReadStore companies) : IQueryHandler<GetCompanyQuery, CompanyResponse>
{
    public async Task<Result<CompanyResponse>> HandleAsync(GetCompanyQuery query, CancellationToken ct) =>
        await companies.GetAsync(ct) is { } company
            ? company
            : CompanyErrors.NotConfigured;
}
