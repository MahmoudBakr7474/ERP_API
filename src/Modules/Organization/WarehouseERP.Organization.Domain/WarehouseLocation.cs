using WarehouseERP.SharedKernel.Domain;

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
}
