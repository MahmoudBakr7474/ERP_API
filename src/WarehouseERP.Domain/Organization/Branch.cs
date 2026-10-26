using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Organization;
public sealed class Branch : MasterEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
