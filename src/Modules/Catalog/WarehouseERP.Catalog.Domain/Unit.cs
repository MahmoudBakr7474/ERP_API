using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

public sealed class Unit : MasterEntity
{
    private Unit() { } // EF Core

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public bool AllowsFraction { get; private set; }

    public static Unit Create(string code, string nameAr, string? nameEn, bool allowsFraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Unit { Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn), AllowsFraction = allowsFraction };
    }

    /// <summary>The code is fixed once created; the names and whether quantities may be fractional change.</summary>
    public void Update(string nameAr, string? nameEn, bool allowsFraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
        AllowsFraction = allowsFraction;
    }
}
