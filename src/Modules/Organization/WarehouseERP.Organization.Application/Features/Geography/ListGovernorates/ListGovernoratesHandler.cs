using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.ListGovernorates;

// A country's governorates, whole: the second step of an address picker.
public sealed class ListGovernoratesHandler(IGeographyReadStore geography)
    : IQueryHandler<ListGovernoratesQuery, IReadOnlyList<GovernorateResponse>>
{
    public async Task<Result<IReadOnlyList<GovernorateResponse>>> HandleAsync(ListGovernoratesQuery query, CancellationToken ct) =>
        await geography.ListGovernoratesAsync(query.CountryCode.Trim().ToUpperInvariant(), query.IsActive, ct) is { } governorates
            ? Result.Success(governorates)
            : GeographyErrors.CountryNotFound;
}
