namespace WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;

/// <summary>What can change after creation; code, type and branch are fixed.</summary>
/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateWarehouseCommand(Guid WarehouseId, string NameAr, string? NameEn, bool AllowsSelling, string RowVersion);
