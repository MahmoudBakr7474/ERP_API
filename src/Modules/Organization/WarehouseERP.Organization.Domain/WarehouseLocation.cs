using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Domain;

public sealed class WarehouseLocation : MasterEntity
{
    private WarehouseLocation() { } // EF Core

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public bool IsDefault { get; private set; }
    // No IsFrozen: active StockCountScope alone owns count freezing.

    internal static WarehouseLocation CreateDefault(Warehouse warehouse) => new()
    {
        Warehouse = warehouse,
        Code = Warehouse.DefaultLocationCode,
        NameAr = "الموقع الافتراضي",
        NameEn = "Default location",
        IsDefault = true,
    };

    internal static WarehouseLocation Create(Warehouse warehouse, string code, string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new WarehouseLocation
        {
            Warehouse = warehouse, Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn),
        };
    }

    /// <summary>The code is fixed once created; only the names change.</summary>
    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }

    /// <summary>The default location is where stock goes when no location is named, so it stays active.</summary>
    public Result SetActive(bool isActive)
    {
        if (!isActive && IsDefault)
            return WarehouseLocationDomainErrors.DefaultMustStayActive;

        if (isActive) Activate();
        else Deactivate();
        return Result.Success();
    }
}
