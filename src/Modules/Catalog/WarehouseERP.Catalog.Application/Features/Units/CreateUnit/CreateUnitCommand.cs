namespace WarehouseERP.Catalog.Application.Features.Units.CreateUnit;

/// <param name="AllowsFraction">Whether a quantity in this unit may have decimals (kilogram) or not (piece).</param>
public sealed record CreateUnitCommand(string Code, string NameAr, string? NameEn, bool AllowsFraction);
