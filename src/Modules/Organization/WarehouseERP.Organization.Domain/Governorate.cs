using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Organization.Domain;

/// <summary>
/// A governorate (province, state) of a country. Its country is fixed once created, so an address that names one of its
/// cities always resolves to the same country.
/// </summary>
public sealed class Governorate : MasterEntity
{
    private Governorate() { } // EF Core

    public Guid CountryId { get; private set; }
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    public static Governorate Create(Guid countryId, string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Governorate { CountryId = countryId, NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn) };
    }

    /// <summary>The country is fixed; only the names change.</summary>
    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }
}
