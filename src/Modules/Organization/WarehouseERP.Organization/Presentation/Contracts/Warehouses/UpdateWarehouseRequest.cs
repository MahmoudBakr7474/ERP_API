namespace WarehouseERP.Organization.Presentation.Contracts.Warehouses;

public sealed record UpdateWarehouseRequest(string NameAr, string? NameEn, bool AllowsSelling, string RowVersion);
