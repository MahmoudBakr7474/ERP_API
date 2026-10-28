using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Organization.Domain;

/// <summary>A city (or district) of a governorate; its governorate is fixed once created.</summary>
public sealed class City : MasterEntity
{
    private City() { } // EF Core

    public Guid GovernorateId { get; private set; }
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    public static City Create(Guid governorateId, string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new City { GovernorateId = governorateId, NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn) };
    }

    /// <summary>The governorate is fixed; only the names change.</summary>
    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }
}
