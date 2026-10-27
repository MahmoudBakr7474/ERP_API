namespace WarehouseERP.Catalog.Application.Features.Units.UpdateUnit;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateUnitCommand(Guid UnitId, string NameAr, string? NameEn, bool AllowsFraction, string RowVersion);
