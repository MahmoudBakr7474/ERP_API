using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Organization.Domain;
public sealed class Warehouse : MasterEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public WarehouseType WarehouseType { get; set; } = global::WarehouseERP.Organization.Contracts.WarehouseType.Physical;
    public bool AllowsSelling { get; set; } = true;
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
}
