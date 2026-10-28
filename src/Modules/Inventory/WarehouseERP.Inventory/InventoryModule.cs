using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;
using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.ListDocumentPolicies;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;
using WarehouseERP.Inventory.Application.Features.Stock.ListOverReservations;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockAvailability;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockBalances;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;
using WarehouseERP.Inventory.Application.Features.StockDocuments;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ApproveStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.CancelStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.GetStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ListStockDocuments;
using WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.RejectStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ReverseStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.SubmitStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.UpdateStockDocument;
using WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;
using WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Infrastructure;
using WarehouseERP.Inventory.Infrastructure.Persistence;
using WarehouseERP.Inventory.Infrastructure.ReadModels;
using WarehouseERP.Inventory.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory;

// Stock documents and their posting, balances, availability, the movement ledger and the company-wide valuation
// (Milestone A, docs/modules/Inventory/V1_DECISIONS.md). Other modules see only Inventory.Contracts: Organization asks
// IWarehouseStockGuard before deactivating a warehouse or a location. The host calls AddInventoryModule and
// MapInventoryEndpoints; ITransactionRunner comes from Persistence.
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateStockDocumentValidator>(includeInternalTypes: true);

        services.AddCommandHandler<CreateStockDocumentCommand, Guid, CreateStockDocumentHandler>();
        services.AddCommandHandler<UpdateStockDocumentCommand, string, UpdateStockDocumentHandler>();
        services.AddQueryHandler<GetStockDocumentQuery, StockDocumentDetailsResponse, GetStockDocumentHandler>();
        services.AddQueryHandler<ListStockDocumentsQuery, PagedResult<StockDocumentSummaryResponse>, ListStockDocumentsHandler>();
        services.AddCommandHandler<SubmitStockDocumentCommand, string, SubmitStockDocumentHandler>();
        services.AddCommandHandler<PostStockDocumentCommand, PostedDocumentResponse, PostStockDocumentHandler>();
        services.AddCommandHandler<ApproveStockDocumentCommand, PostedDocumentResponse, ApproveStockDocumentHandler>();
        services.AddCommandHandler<RejectStockDocumentCommand, string, RejectStockDocumentHandler>();
        services.AddCommandHandler<CancelStockDocumentCommand, string, CancelStockDocumentHandler>();
        services.AddCommandHandler<ReverseStockDocumentCommand, Guid, ReverseStockDocumentHandler>();

        services.AddQueryHandler<ListStockBalancesQuery, PagedResult<StockBalanceResponse>, ListStockBalancesHandler>();
        services.AddQueryHandler<ListStockAvailabilityQuery, PagedResult<StockAvailabilityResponse>, ListStockAvailabilityHandler>();
        services.AddQueryHandler<ListStockMovementsQuery, PagedResult<StockMovementResponse>, ListStockMovementsHandler>();
        services.AddQueryHandler<ListOverReservationsQuery, PagedResult<OverReservationResponse>, ListOverReservationsHandler>();

        services.AddQueryHandler<ListInventoryValuationQuery, PagedResult<InventoryValuationResponse>, ListInventoryValuationHandler>();
        services.AddQueryHandler<ListCostEntriesQuery, PagedResult<InventoryCostEntryResponse>, ListCostEntriesHandler>();

        services.AddQueryHandler<ListDocumentPoliciesQuery, IReadOnlyList<DocumentPolicyResponse>, ListDocumentPoliciesHandler>();
        services.AddCommandHandler<UpdateDocumentPolicyCommand, string, UpdateDocumentPolicyHandler>();

        services.AddScoped<StockReferences>();
        services.AddScoped<StockDocumentPosting>();
        services.AddScoped<IStockDocumentRepository, StockDocumentRepository>();
        services.AddScoped<IDocumentPolicyRepository, DocumentPolicyRepository>();
        services.AddScoped<IInventoryLocks, InventoryLocks>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IInventoryValuation, InventoryValuation>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<ReferenceNames>();
        services.AddScoped<IStockDocumentReadStore, StockDocumentReadStore>();
        services.AddScoped<IStockReadStore, StockReadStore>();
        services.AddScoped<IValuationReadStore, ValuationReadStore>();
        services.AddScoped<IWarehouseStockGuard, WarehouseStockGuard>();

        services.AddPermissions("Inventory", InventoryPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app) =>
        app.MapStockDocumentEndpoints()
            .MapStockEndpoints()
            .MapValuationEndpoints()
            .MapDocumentPolicyEndpoints();
}
