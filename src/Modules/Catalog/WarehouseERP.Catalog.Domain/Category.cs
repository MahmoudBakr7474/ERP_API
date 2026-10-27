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

    // Deeper cycles (A -> B -> A) are checked by the category commands, which can see the ancestors; the
    // database only refuses a category that is its own parent.
    public static Category Create(string code, string nameAr, string? nameEn, Guid? parentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Category { Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn), ParentId = parentId };
    }

    /// <summary>The code is fixed once created; only the names change.</summary>
    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }

    /// <summary>The caller has checked that the new parent is active and is neither this category nor below it.</summary>
    public void MoveTo(Guid? parentId) => ParentId = parentId;
}
