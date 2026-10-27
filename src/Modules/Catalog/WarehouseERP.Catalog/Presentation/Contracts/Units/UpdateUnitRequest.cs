namespace WarehouseERP.Catalog.Presentation.Contracts.Units;

public sealed record UpdateUnitRequest(string NameAr, string? NameEn, bool AllowsFraction, string RowVersion);
