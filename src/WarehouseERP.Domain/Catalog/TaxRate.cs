using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public sealed class TaxRate : MasterEntity
{
    public Guid TaxCodeId { get; set; }
    public TaxCode TaxCode { get; set; } = null!;
    // Fractional rate, e.g. 0.14 = 14%.
    public decimal Rate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; } // exclusive
}
