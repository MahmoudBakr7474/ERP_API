namespace WarehouseERP.Organization.Presentation.Contracts.WarehouseLocations;

public sealed record UpdateWarehouseLocationRequest(string NameAr, string? NameEn, string RowVersion);
