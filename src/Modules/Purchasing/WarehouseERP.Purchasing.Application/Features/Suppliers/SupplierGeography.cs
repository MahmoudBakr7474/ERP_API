using WarehouseERP.Organization.Contracts;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers;

// The supplier's country and its addresses' cities come from Organization's lookups. A new one must be in use (active;
// for a city, its governorate and country too). One the supplier already has may stay after it was deactivated, so an
// existing supplier can still be edited.
internal static class SupplierGeography
{
    public static async Task<Result> CheckAsync(
        IGeographyDirectory geography, Supplier? current, string countryCode, IReadOnlyList<SupplierAddressRequest>? addresses,
        CancellationToken ct)
    {
        var code = countryCode.Trim().ToUpperInvariant();
        if (current?.CountryCode != code && await geography.FindCountryAsync(code, ct) is not { IsActive: true })
            return SupplierErrors.CountryNotFound;

        var requested = (addresses ?? []).Select(a => a.CityId).ToList();
        if (requested.Count == 0)
            return Result.Success();
        HashSet<Guid> kept = current is null ? [] : [.. current.Addresses.Select(a => a.CityId)];
        var cities = (await geography.FindCitiesAsync(requested, ct)).ToDictionary(c => c.Id);
        for (var i = 0; i < requested.Count; i++)
        {
            if (!cities.TryGetValue(requested[i], out var city) || !(city.IsUsable || kept.Contains(city.Id)))
                return SupplierErrors.CityNotFound(i + 1);
        }

        return Result.Success();
    }
}
