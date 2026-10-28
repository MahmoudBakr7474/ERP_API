using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Organization.Domain;

/// <summary>
/// A country of the address lookups, keyed by its ISO 3166 alpha-2 code, which is fixed once created. Governorates hang
/// off it and cities off them. An inactive country is not offered for new addresses; addresses already using it keep it.
/// </summary>
public sealed class Country : MasterEntity
{
    public const int CodeLength = 2;

    private Country() { } // EF Core

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    public static Country Create(string code, string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Country { Code = code.Trim().ToUpperInvariant(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn) };
    }

    /// <summary>The code is fixed; only the names change.</summary>
    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }
}
