using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;

/// <summary>The location import's unit of work over the shared ErpDbContext (no generic repository).</summary>
public interface ILocationImportStore
{
    /// <param name="branchCodes">The branch codes the file names (case is ignored, as by the database).</param>
    Task<LocationImportSnapshot> LoadAsync(IReadOnlyCollection<string> branchCodes, CancellationToken ct);

    /// <summary>
    /// The single save point: every new and renamed location in one transaction. A location code taken meanwhile, or a
    /// location changed since it was loaded, rolls everything back with its usual error.
    /// </summary>
    Task<Result> SaveAsync(CancellationToken ct);
}
