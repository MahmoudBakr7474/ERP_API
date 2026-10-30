using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Companies;

// The company's address names a city of the lookups, like a supplier's address does.
internal static class CompanyCity
{
    public static async Task<bool> IsUsableAsync(IGeographyDirectory geography, Guid cityId, CancellationToken ct) =>
        (await geography.FindCitiesAsync([cityId], ct)) is [{ IsUsable: true }];
}
