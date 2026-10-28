namespace WarehouseERP.Organization.Contracts;

/// <summary>Read-only view of the company for other modules.</summary>
public interface ICompanyDirectory
{
    /// <summary>The company, or null until it is set up.</summary>
    Task<CompanyInfo?> GetAsync(CancellationToken ct);
}
