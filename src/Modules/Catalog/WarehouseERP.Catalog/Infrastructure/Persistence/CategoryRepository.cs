using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class CategoryRepository(DbContext db) : ICategoryRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration 01.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Categories_Code"] = CategoryErrors.CodeTaken,
    };

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        db.Set<Category>().AnyAsync(x => x.Code == code, ct);

    public Task<bool> IsActiveAsync(Guid categoryId, CancellationToken ct) =>
        db.Set<Category>().AnyAsync(x => x.Id == categoryId && x.IsActive, ct);

    public async Task<bool> IsInSubtreeAsync(Guid categoryId, Guid candidateId, CancellationToken ct)
    {
        // Categories are few, so one read of every (id, parent) pair, then a walk up from the candidate. The walk
        // stops after one pass over every category even if bad data held a loop.
        var parents = await db.Set<Category>().AsNoTracking()
            .Select(x => new { x.Id, x.ParentId })
            .ToDictionaryAsync(x => x.Id, x => x.ParentId, ct);
        Guid? current = candidateId;
        for (var steps = 0; current is { } id && steps <= parents.Count; steps++)
        {
            if (id == categoryId)
                return true;
            current = parents.GetValueOrDefault(id);
        }
        return false;
    }

    public Task<bool> HasActiveChildrenAsync(Guid categoryId, CancellationToken ct) =>
        db.Set<Category>().AnyAsync(x => x.ParentId == categoryId && x.IsActive, ct);

    public Task<Result> AddAsync(Category category, CancellationToken ct)
    {
        db.Set<Category>().Add(category);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    public async Task<Category?> GetForUpdateAsync(Guid categoryId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var category = await db.Set<Category>().SingleOrDefaultAsync(x => x.Id == categoryId, ct);
        if (category is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(category).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return category;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
