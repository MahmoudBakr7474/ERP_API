using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportBranches;

/// <summary>The branch directory with the branch list's filters and order.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportBranchesQuery(
    string? Search, bool? IsActive, bool? HasWarehouses, OrganizationSort? Sort, bool Descending, int MaxRows);
