using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Organization.Domain;
public sealed class Branch : MasterEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
