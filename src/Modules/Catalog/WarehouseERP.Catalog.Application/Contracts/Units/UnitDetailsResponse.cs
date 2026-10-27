namespace WarehouseERP.Catalog.Application.Contracts.Units;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record UnitDetailsResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool AllowsFraction, bool IsActive, string RowVersion);
