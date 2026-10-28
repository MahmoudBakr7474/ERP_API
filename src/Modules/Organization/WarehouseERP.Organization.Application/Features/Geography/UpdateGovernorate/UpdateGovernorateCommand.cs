namespace WarehouseERP.Organization.Application.Features.Geography.UpdateGovernorate;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateGovernorateCommand(Guid GovernorateId, string NameAr, string? NameEn, string RowVersion);
