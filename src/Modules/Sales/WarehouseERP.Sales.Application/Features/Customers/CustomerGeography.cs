using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers;

// The customer's country and its addresses' cities come from Organization's lookups (ADR 0017). A new one must be in use
// (active; for a city, its governorate and country too). One the customer already has may stay after it was deactivated,
// so an existing customer can still be edited.
internal static class CustomerGeography
{
    public static async Task<Result> CheckAsync(
        IGeographyDirectory geography, Customer? current, string countryCode, IReadOnlyList<CustomerAddressRequest>? addresses,
        CancellationToken ct)
    {
        var code = countryCode.Trim().ToUpperInvariant();
        if (current?.CountryCode != code && await geography.FindCountryAsync(code, ct) is not { IsActive: true })
            return CustomerErrors.CountryNotFound;
        var requested = (addresses ?? []).Select(a => a.CityId).ToList();
        if (requested.Count == 0)
            return Result.Success();
        HashSet<Guid> kept = current is null ? [] : [.. current.Addresses.Select(a => a.CityId)];
        var cities = (await geography.FindCitiesAsync(requested, ct)).ToDictionary(c => c.Id);
        for (var i = 0; i < requested.Count; i++)
        {
            if (!cities.TryGetValue(requested[i], out var city) || !(city.IsUsable || kept.Contains(city.Id)))
                return CustomerErrors.CityNotFound(i + 1);
        }
        return Result.Success();
    }
}
