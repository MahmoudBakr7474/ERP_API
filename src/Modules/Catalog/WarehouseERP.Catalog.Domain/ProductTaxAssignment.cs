using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Catalog.Domain;
public sealed class ProductTaxAssignment : MasterEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid TaxCodeId { get; set; }
    public TaxCode TaxCode { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; } // exclusive
}
