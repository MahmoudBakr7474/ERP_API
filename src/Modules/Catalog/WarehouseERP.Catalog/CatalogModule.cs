using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.Catalog.Application.Features.Categories.CreateCategory;
using WarehouseERP.Catalog.Application.Features.Categories.GetCategory;
using WarehouseERP.Catalog.Application.Features.Categories.ListCategories;
using WarehouseERP.Catalog.Application.Features.Categories.SetCategoryActive;
using WarehouseERP.Catalog.Application.Features.Categories.UpdateCategory;
using WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;
using WarehouseERP.Catalog.Application.Features.ProductBarcodes.SetProductBarcodeActive;
using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Application.Features.Products.GetProduct;
using WarehouseERP.Catalog.Application.Features.Products.ListProducts;
using WarehouseERP.Catalog.Application.Features.Products.LookupProduct;
using WarehouseERP.Catalog.Application.Features.Products.SetProductActive;
using WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;
using WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;
using WarehouseERP.Catalog.Application.Features.ProductUnits.SetProductUnitActive;
using WarehouseERP.Catalog.Application.Features.Units.CreateUnit;
using WarehouseERP.Catalog.Application.Features.Units.GetUnit;
using WarehouseERP.Catalog.Application.Features.Units.ListUnits;
using WarehouseERP.Catalog.Application.Features.Units.SetUnitActive;
using WarehouseERP.Catalog.Application.Features.Units.UpdateUnit;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Infrastructure;
using WarehouseERP.Catalog.Infrastructure.Persistence;
using WarehouseERP.Catalog.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog;

// Units, categories, products with their units and barcodes. Tax definitions and external item codes keep their
// tables for the Sales/e-invoicing phase. The host calls AddCatalogModule and MapCatalogEndpoints; nothing else of
// this module is public to it.
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>(includeInternalTypes: true);

        services.AddQueryHandler<ListUnitsQuery, PagedResult<UnitSummaryResponse>, ListUnitsHandler>();
        services.AddQueryHandler<GetUnitQuery, UnitDetailsResponse, GetUnitHandler>();
        services.AddCommandHandler<CreateUnitCommand, Guid, CreateUnitHandler>();
        services.AddCommandHandler<UpdateUnitCommand, string, UpdateUnitHandler>();
        services.AddCommandHandler<SetUnitActiveCommand, Done, SetUnitActiveHandler>();

        services.AddQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>, ListCategoriesHandler>();
        services.AddQueryHandler<GetCategoryQuery, CategoryDetailsResponse, GetCategoryHandler>();
        services.AddCommandHandler<CreateCategoryCommand, Guid, CreateCategoryHandler>();
        services.AddCommandHandler<UpdateCategoryCommand, string, UpdateCategoryHandler>();
        services.AddCommandHandler<SetCategoryActiveCommand, Done, SetCategoryActiveHandler>();

        services.AddCommandHandler<CreateProductCommand, Guid, CreateProductHandler>();
        services.AddCommandHandler<UpdateProductCommand, string, UpdateProductHandler>();
        services.AddCommandHandler<SetProductActiveCommand, Done, SetProductActiveHandler>();
        services.AddQueryHandler<GetProductQuery, ProductDetailsResponse, GetProductHandler>();
        services.AddQueryHandler<ListProductsQuery, PagedResult<ProductSummaryResponse>, ListProductsHandler>();
        services.AddQueryHandler<LookupProductQuery, ProductLookupResponse, LookupProductHandler>();

        services.AddCommandHandler<AddProductUnitCommand, Guid, AddProductUnitHandler>();
        services.AddCommandHandler<SetProductUnitActiveCommand, Done, SetProductUnitActiveHandler>();
        services.AddCommandHandler<AddProductBarcodeCommand, Guid, AddProductBarcodeHandler>();
        services.AddCommandHandler<SetProductBarcodeActiveCommand, Done, SetProductBarcodeActiveHandler>();

        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitReadStore, UnitReadStore>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryReadStore, CategoryReadStore>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReadStore, ProductReadStore>();
        services.AddScoped<IProductCatalog, ProductCatalog>();

        services.AddPermissions("Catalog", CatalogPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app) =>
        app.MapUnitEndpoints()
            .MapCategoryEndpoints()
            .MapProductEndpoints()
            .MapProductUnitEndpoints()
            .MapProductBarcodeEndpoints();
}
