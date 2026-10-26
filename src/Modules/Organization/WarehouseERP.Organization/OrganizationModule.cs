using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Organization.Application;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization;

// Companies, branches, warehouses and their locations.
public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<WarehouseCommands>();
        services.AddScoped<IWarehouseDirectory, WarehouseDirectory>();
        return services;
    }
}
