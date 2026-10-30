using WarehouseERP.Organization.Application.Contracts.Reports;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportStructure;

/// <param name="Locations">Every location of the listed warehouses (empty unless asked for), ordered as the directory.</param>
public sealed record OrganizationStructureExport(
    IReadOnlyList<StructureBranchResponse> Branches, IReadOnlyList<LocationDirectoryResponse> Locations);
