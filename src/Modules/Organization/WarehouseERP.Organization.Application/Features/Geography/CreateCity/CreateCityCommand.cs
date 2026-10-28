namespace WarehouseERP.Organization.Application.Features.Geography.CreateCity;

/// <param name="GovernorateId">An active governorate; fixed once created.</param>
public sealed record CreateCityCommand(Guid GovernorateId, string NameAr, string? NameEn);
