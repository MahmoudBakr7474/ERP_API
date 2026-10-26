using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.Organization.Application.Features.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.GetWarehouse;
using WarehouseERP.Organization.Application.Features.ListWarehouses;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Infrastructure;
using WarehouseERP.Organization.Infrastructure.Persistence;
using WarehouseERP.Organization.Presentation;

namespace WarehouseERP.Organization;

// Companies, branches, warehouses and their locations. The host calls AddOrganizationModule and
// MapOrganizationEndpoints; nothing else of this module is public to it.
public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateWarehouseValidator>(includeInternalTypes: true);

        services.AddCommandHandler<CreateWarehouseCommand, Guid, CreateWarehouseHandler>();
        services.AddQueryHandler<GetWarehouseQuery, WarehouseDetails, GetWarehouseHandler>();
        services.AddQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummary>, ListWarehousesHandler>();

        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseReadStore, WarehouseReadStore>();
        services.AddScoped<IWarehouseDirectory, WarehouseDirectory>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app) =>
        app.MapWarehouseEndpoints();
}
