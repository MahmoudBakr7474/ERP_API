using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Organization;
public sealed class WarehouseLocation : MasterEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public bool IsDefault { get; set; }
    // No IsFrozen: active StockCountScope alone owns count freezing.
}
