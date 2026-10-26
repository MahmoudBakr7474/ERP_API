using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Catalog.Domain;
public sealed class Category : MasterEntity
{
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
}
