using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reconciliation;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.Inventory.Application.Contracts.Reconciliation;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.Inventory.Application.Features.Counts.ApproveStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.CancelStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.CreateStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;
using WarehouseERP.Inventory.Application.Features.Counts.GetStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.ListStockCounts;
using WarehouseERP.Inventory.Application.Features.Counts.RejectStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.StartStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.SubmitStockCount;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.ListDocumentPolicies;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;
using WarehouseERP.Inventory.Application.Features.Reconciliation.GetReconciliation;
using WarehouseERP.Inventory.Application.Features.Stock.ListOverReservations;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockAvailability;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockBalances;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;
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
using WarehouseERP.Inventory.Application.Features.Transfers;
using WarehouseERP.Inventory.Application.Features.Transfers.ApproveStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CancelStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CloseStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CreateStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.DispatchStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.GetStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.ListStockTransfers;
using WarehouseERP.Inventory.Application.Features.Transfers.ListTransferDestinations;
using WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.RejectStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.SubmitStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.UpdateStockTransfer;
using WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;
using WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Infrastructure;
using WarehouseERP.Inventory.Infrastructure.Persistence;
using WarehouseERP.Inventory.Infrastructure.ReadModels;
using WarehouseERP.Inventory.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory;

// Stock documents and their posting, balances, availability, the movement ledger and the company-wide valuation
// (Milestone A), transfers through transit warehouses, stock counts and the reconciliation report (Milestone B); see
// docs/modules/Inventory/V1_DECISIONS.md and ADR 0015. Other modules see only Inventory.Contracts: Organization asks
// IWarehouseStockGuard before deactivating a warehouse or a location and opens transit locations through
// ITransitLocations. The host calls AddInventoryModule and MapInventoryEndpoints; ITransactionRunner comes from
// Persistence.
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

        services.AddCommandHandler<CreateStockTransferCommand, Guid, CreateStockTransferHandler>();
        services.AddCommandHandler<UpdateStockTransferCommand, string, UpdateStockTransferHandler>();
        services.AddQueryHandler<GetStockTransferQuery, StockTransferDetailsResponse, GetStockTransferHandler>();
        services.AddQueryHandler<ListStockTransfersQuery, PagedResult<StockTransferSummaryResponse>, ListStockTransfersHandler>();
        services.AddQueryHandler<ListTransferDestinationsQuery, IReadOnlyList<TransferDestinationResponse>, ListTransferDestinationsHandler>();
        services.AddCommandHandler<SubmitStockTransferCommand, string, SubmitStockTransferHandler>();
        services.AddCommandHandler<RejectStockTransferCommand, string, RejectStockTransferHandler>();
        services.AddCommandHandler<CancelStockTransferCommand, string, CancelStockTransferHandler>();
        services.AddCommandHandler<DispatchStockTransferCommand, StockTransferPostedResponse, DispatchStockTransferHandler>();
        services.AddCommandHandler<ApproveStockTransferCommand, StockTransferPostedResponse, ApproveStockTransferHandler>();
        services.AddCommandHandler<ReceiveStockTransferCommand, StockTransferPostedResponse, ReceiveStockTransferHandler>();
        services.AddCommandHandler<CloseStockTransferCommand, Guid, CloseStockTransferHandler>();

        services.AddCommandHandler<CreateStockCountCommand, Guid, CreateStockCountHandler>();
        services.AddQueryHandler<GetStockCountQuery, StockCountDetailsResponse, GetStockCountHandler>();
        services.AddQueryHandler<ListStockCountsQuery, PagedResult<StockCountSummaryResponse>, ListStockCountsHandler>();
        services.AddCommandHandler<StartStockCountCommand, string, StartStockCountHandler>();
        services.AddCommandHandler<EnterStockCountLinesCommand, string, EnterStockCountLinesHandler>();
        services.AddCommandHandler<SubmitStockCountCommand, string, SubmitStockCountHandler>();
        services.AddCommandHandler<RejectStockCountCommand, string, RejectStockCountHandler>();
        services.AddCommandHandler<CancelStockCountCommand, string, CancelStockCountHandler>();
        services.AddCommandHandler<ApproveStockCountCommand, StockCountApprovalResponse, ApproveStockCountHandler>();

        services.AddQueryHandler<GetReconciliationQuery, ReconciliationReport, GetReconciliationHandler>();

        services.AddScoped<StockReferences>();
        services.AddScoped<StockDocumentPosting>();
        services.AddScoped<TransferDispatching>();
        services.AddScoped<IStockDocumentRepository, StockDocumentRepository>();
        services.AddScoped<IStockTransferRepository, StockTransferRepository>();
        services.AddScoped<IStockCountRepository, StockCountRepository>();
        services.AddScoped<IStockFreezes, StockFreezes>();
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
        services.AddScoped<IStockTransferReadStore, StockTransferReadStore>();
        services.AddScoped<IStockCountReadStore, StockCountReadStore>();
        services.AddScoped<IReconciliationReadStore, ReconciliationReadStore>();
        services.AddScoped<IWarehouseStockGuard, WarehouseStockGuard>();

        services.AddPermissions("Inventory", InventoryPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app) =>
        app.MapStockDocumentEndpoints()
            .MapStockEndpoints()
            .MapValuationEndpoints()
            .MapDocumentPolicyEndpoints()
            .MapStockTransferEndpoints()
            .MapStockCountEndpoints()
            .MapReconciliationEndpoints();
}
