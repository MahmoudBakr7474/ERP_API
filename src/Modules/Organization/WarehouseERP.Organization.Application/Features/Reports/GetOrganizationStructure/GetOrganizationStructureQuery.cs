namespace WarehouseERP.Organization.Application.Features.Reports.GetOrganizationStructure;

/// <param name="IsActive">Only active (or only inactive) branches, warehouses and counted locations; null: all.</param>
public sealed record GetOrganizationStructureQuery(bool? IsActive);
