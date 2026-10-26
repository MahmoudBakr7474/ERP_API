using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

public sealed class Category : MasterEntity
{
    private readonly List<Category> _children = [];

    private Category() { } // EF Core

    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public IReadOnlyCollection<Category> Children => _children;
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    // Deeper cycles (A -> B -> A) are checked by the future category commands; the database
    // only refuses a category that is its own parent.
    public static Category Create(string code, string nameAr, string? nameEn, Guid? parentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Category { Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn), ParentId = parentId };
    }
}
