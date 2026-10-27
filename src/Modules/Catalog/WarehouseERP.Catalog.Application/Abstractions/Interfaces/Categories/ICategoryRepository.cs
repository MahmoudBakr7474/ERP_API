using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;

/// <summary>Write side of categories; implemented over the shared ErpDbContext (no generic repository).</summary>
public interface ICategoryRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    Task<bool> IsActiveAsync(Guid categoryId, CancellationToken ct);

    /// <summary>True when <paramref name="candidateId"/> is the category itself or lies anywhere below it.</summary>
    Task<bool> IsInSubtreeAsync(Guid categoryId, Guid candidateId, CancellationToken ct);

    Task<bool> HasActiveChildrenAsync(Guid categoryId, CancellationToken ct);

    /// <summary>Save point for a new category. A known unique-key violation comes back as its error, not an exception.</summary>
    Task<Result> AddAsync(Category category, CancellationToken ct);

    /// <summary>
    /// The category, tracked for changes, or null. With a row version, the save fails with a conflict unless the
    /// category still has it.
    /// </summary>
    Task<Category?> GetForUpdateAsync(Guid categoryId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>Save point for changes to categories loaded with <see cref="GetForUpdateAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
