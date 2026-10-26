using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Organization;
public enum WarehouseType : byte { Physical = 1, Transit = 2 }
public sealed class Warehouse : MasterEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public WarehouseType WarehouseType { get; set; } = global::WarehouseERP.Domain.Organization.WarehouseType.Physical;
    public bool AllowsSelling { get; set; } = true;
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
}
