using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Catalog.Domain;
public sealed class Unit : MasterEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public bool AllowsFraction { get; set; }
}
