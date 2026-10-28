namespace WarehouseERP.Organization.Application.Contracts.Geography;

/// <param name="RowVersion">Base64; send it back to rename.</param>
public sealed record CityResponse(Guid Id, Guid GovernorateId, string NameAr, string? NameEn, bool IsActive, string RowVersion);
