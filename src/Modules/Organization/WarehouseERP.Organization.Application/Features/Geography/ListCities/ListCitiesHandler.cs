using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.ListCities;

// A governorate's cities, whole: the last step of an address picker.
public sealed class ListCitiesHandler(IGeographyReadStore geography)
    : IQueryHandler<ListCitiesQuery, IReadOnlyList<CityResponse>>
{
    public async Task<Result<IReadOnlyList<CityResponse>>> HandleAsync(ListCitiesQuery query, CancellationToken ct) =>
        await geography.ListCitiesAsync(query.GovernorateId, query.IsActive, ct) is { } cities
            ? Result.Success(cities)
            : GeographyErrors.GovernorateNotFound;
}
