namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.UpdateWarehouseLocation;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateWarehouseLocationCommand(Guid WarehouseId, Guid LocationId, string NameAr, string? NameEn, string RowVersion);
