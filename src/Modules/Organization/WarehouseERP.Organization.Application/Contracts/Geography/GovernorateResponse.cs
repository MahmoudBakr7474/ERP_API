namespace WarehouseERP.Organization.Application.Contracts.Geography;

/// <param name="RowVersion">Base64; send it back to rename.</param>
public sealed record GovernorateResponse(Guid Id, string CountryCode, string NameAr, string? NameEn, bool IsActive, string RowVersion);
