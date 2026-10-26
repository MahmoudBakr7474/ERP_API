using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public sealed class Unit : MasterEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public bool AllowsFraction { get; set; }
}
