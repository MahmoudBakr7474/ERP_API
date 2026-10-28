using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.ListCountries;

// Every country in one call (a few hundred rows), for address pickers and the lookup screen.
public sealed class ListCountriesHandler(IGeographyReadStore geography)
    : IQueryHandler<ListCountriesQuery, IReadOnlyList<CountryResponse>>
{
    public async Task<Result<IReadOnlyList<CountryResponse>>> HandleAsync(ListCountriesQuery query, CancellationToken ct) =>
        Result.Success(await geography.ListCountriesAsync(query.IsActive, ct));
}
