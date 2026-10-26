using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public enum TaxType : byte { ValueAdded = 1, Other = 2 }
public sealed class TaxCode : MasterEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public TaxType TaxType { get; set; } = global::WarehouseERP.Domain.Catalog.TaxType.ValueAdded;
}
