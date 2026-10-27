using WarehouseERP.Organization.Application.Contracts.Companies;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;

/// <summary>Read side of the company: a projection, no tracking.</summary>
public interface ICompanyReadStore
{
    /// <summary>The company, or null until it is set up.</summary>
    Task<CompanyResponse?> GetAsync(CancellationToken ct);
}
