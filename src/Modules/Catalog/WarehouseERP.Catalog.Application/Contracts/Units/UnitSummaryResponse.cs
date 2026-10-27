namespace WarehouseERP.Catalog.Application.Contracts.Units;

public sealed record UnitSummaryResponse(Guid Id, string Code, string NameAr, string? NameEn, bool AllowsFraction, bool IsActive);
