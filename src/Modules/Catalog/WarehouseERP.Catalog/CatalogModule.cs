using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Catalog.Application;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog;

// Products, units, barcodes, external item codes and tax definitions.
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ProductCommands>();
        services.AddScoped<IProductCatalog, ProductCatalog>();
        return services;
    }
}
