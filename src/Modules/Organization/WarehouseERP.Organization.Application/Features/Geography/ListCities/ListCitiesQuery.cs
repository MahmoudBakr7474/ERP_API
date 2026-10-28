namespace WarehouseERP.Organization.Application.Features.Geography.ListCities;

/// <param name="IsActive">Only active or only inactive cities; null for all.</param>
public sealed record ListCitiesQuery(Guid GovernorateId, bool? IsActive);
