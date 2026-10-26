using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Catalog.Application.Abstractions;
using WarehouseERP.Catalog.Application.Features.CreateProduct;
using WarehouseERP.Catalog.Application.Features.GetProduct;
using WarehouseERP.Catalog.Application.Features.ListProducts;
using WarehouseERP.Catalog.Application.Features.UpdateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Infrastructure;
using WarehouseERP.Catalog.Infrastructure.Persistence;
using WarehouseERP.Catalog.Presentation;

namespace WarehouseERP.Catalog;

// Products, units, barcodes, external item codes and tax definitions. The host calls
// AddCatalogModule and MapCatalogEndpoints; nothing else of this module is public to it.
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>(includeInternalTypes: true);

        services.AddCommandHandler<CreateProductCommand, Guid, CreateProductHandler>();
        services.AddCommandHandler<UpdateProductCommand, string, UpdateProductHandler>();
        services.AddQueryHandler<GetProductQuery, ProductDetails, GetProductHandler>();
        services.AddQueryHandler<ListProductsQuery, PagedResult<ProductSummary>, ListProductsHandler>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReadStore, ProductReadStore>();
        services.AddScoped<IProductCatalog, ProductCatalog>();

        services.AddPermissions("Catalog", CatalogPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app) =>
        app.MapProductEndpoints();
}
