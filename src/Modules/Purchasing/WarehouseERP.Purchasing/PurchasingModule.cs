using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.GetGoodsReceipt;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ListGoodsReceipts;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ApprovePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CancelPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ClosePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CreatePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.GetPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ListPurchaseOrders;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.RejectPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.SubmitPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.UpdatePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ApprovePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CancelPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ClosePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CreatePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.GetPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ListPurchaseRequests;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.RejectPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.SubmitPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.UpdatePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.ListPurchasingPolicies;
using WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.UpdatePurchasingPolicy;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ApproveSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CancelSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CreateSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.GetSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ListSupplierInvoices;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.PostSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.RejectSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.SubmitSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.UpdateSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.ApproveSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.CancelSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.CreateSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.GetSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.ListSupplierReturns;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.RejectSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.SubmitSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.UpdateSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.Suppliers.CreateSupplier;
using WarehouseERP.Purchasing.Application.Features.Suppliers.FindSimilarSuppliers;
using WarehouseERP.Purchasing.Application.Features.Suppliers.GetSupplier;
using WarehouseERP.Purchasing.Application.Features.Suppliers.ListSuppliers;
using WarehouseERP.Purchasing.Application.Features.Suppliers.SetSupplierActive;
using WarehouseERP.Purchasing.Application.Features.Suppliers.UpdateSupplier;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Infrastructure.Persistence;
using WarehouseERP.Purchasing.Infrastructure.ReadModels;
using WarehouseERP.Purchasing.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing;

// Suppliers, purchase requests and orders, goods receipts, supplier invoices and returns (docs/modules/Purchasing and
// ADR 0016). Goods move in and out of stock only through Inventory's IPurchaseStockPosting; products and units come
// from Catalog, warehouses, locations, branches and the company from Organization, display names from Identity, all
// through their Contracts. The host calls AddPurchasingModule and MapPurchasingEndpoints; ITransactionRunner comes from
// Persistence.
public static class PurchasingModule
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateSupplierValidator>(includeInternalTypes: true);

        services.AddCommandHandler<CreateSupplierCommand, Guid, CreateSupplierHandler>();
        services.AddCommandHandler<UpdateSupplierCommand, string, UpdateSupplierHandler>();
        services.AddCommandHandler<SetSupplierActiveCommand, Done, SetSupplierActiveHandler>();
        services.AddQueryHandler<GetSupplierQuery, SupplierDetailsResponse, GetSupplierHandler>();
        services.AddQueryHandler<ListSuppliersQuery, PagedResult<SupplierSummaryResponse>, ListSuppliersHandler>();
        services.AddQueryHandler<FindSimilarSuppliersQuery, IReadOnlyList<SimilarSupplierResponse>, FindSimilarSuppliersHandler>();

        services.AddQueryHandler<ListPurchasingPoliciesQuery, IReadOnlyList<PurchasingPolicyResponse>, ListPurchasingPoliciesHandler>();
        services.AddCommandHandler<UpdatePurchasingPolicyCommand, string, UpdatePurchasingPolicyHandler>();

        services.AddCommandHandler<CreatePurchaseRequestCommand, Guid, CreatePurchaseRequestHandler>();
        services.AddCommandHandler<UpdatePurchaseRequestCommand, string, UpdatePurchaseRequestHandler>();
        services.AddCommandHandler<SubmitPurchaseRequestCommand, string, SubmitPurchaseRequestHandler>();
        services.AddCommandHandler<ApprovePurchaseRequestCommand, string, ApprovePurchaseRequestHandler>();
        services.AddCommandHandler<RejectPurchaseRequestCommand, string, RejectPurchaseRequestHandler>();
        services.AddCommandHandler<CancelPurchaseRequestCommand, string, CancelPurchaseRequestHandler>();
        services.AddCommandHandler<ClosePurchaseRequestCommand, string, ClosePurchaseRequestHandler>();
        services.AddQueryHandler<GetPurchaseRequestQuery, PurchaseRequestDetailsResponse, GetPurchaseRequestHandler>();
        services.AddQueryHandler<ListPurchaseRequestsQuery, PagedResult<PurchaseRequestSummaryResponse>, ListPurchaseRequestsHandler>();

        services.AddCommandHandler<CreatePurchaseOrderCommand, Guid, CreatePurchaseOrderHandler>();
        services.AddCommandHandler<UpdatePurchaseOrderCommand, string, UpdatePurchaseOrderHandler>();
        services.AddCommandHandler<SubmitPurchaseOrderCommand, string, SubmitPurchaseOrderHandler>();
        services.AddCommandHandler<ApprovePurchaseOrderCommand, string, ApprovePurchaseOrderHandler>();
        services.AddCommandHandler<RejectPurchaseOrderCommand, string, RejectPurchaseOrderHandler>();
        services.AddCommandHandler<CancelPurchaseOrderCommand, string, CancelPurchaseOrderHandler>();
        services.AddCommandHandler<ClosePurchaseOrderCommand, string, ClosePurchaseOrderHandler>();
        services.AddQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDetailsResponse, GetPurchaseOrderHandler>();
        services.AddQueryHandler<ListPurchaseOrdersQuery, PagedResult<PurchaseOrderSummaryResponse>, ListPurchaseOrdersHandler>();

        services.AddCommandHandler<ReceiveGoodsCommand, GoodsReceiptPostedResponse, ReceiveGoodsHandler>();
        services.AddQueryHandler<GetGoodsReceiptQuery, GoodsReceiptDetailsResponse, GetGoodsReceiptHandler>();
        services.AddQueryHandler<ListGoodsReceiptsQuery, PagedResult<GoodsReceiptSummaryResponse>, ListGoodsReceiptsHandler>();

        services.AddCommandHandler<CreateSupplierInvoiceCommand, Guid, CreateSupplierInvoiceHandler>();
        services.AddCommandHandler<UpdateSupplierInvoiceCommand, string, UpdateSupplierInvoiceHandler>();
        services.AddCommandHandler<SubmitSupplierInvoiceCommand, string, SubmitSupplierInvoiceHandler>();
        services.AddCommandHandler<PostSupplierInvoiceCommand, SupplierInvoicePostedResponse, PostSupplierInvoiceHandler>();
        services.AddCommandHandler<ApproveSupplierInvoiceCommand, SupplierInvoicePostedResponse, ApproveSupplierInvoiceHandler>();
        services.AddCommandHandler<RejectSupplierInvoiceCommand, string, RejectSupplierInvoiceHandler>();
        services.AddCommandHandler<CancelSupplierInvoiceCommand, string, CancelSupplierInvoiceHandler>();
        services.AddQueryHandler<GetSupplierInvoiceQuery, SupplierInvoiceDetailsResponse, GetSupplierInvoiceHandler>();
        services.AddQueryHandler<ListSupplierInvoicesQuery, PagedResult<SupplierInvoiceSummaryResponse>, ListSupplierInvoicesHandler>();

        services.AddCommandHandler<CreateSupplierReturnCommand, Guid, CreateSupplierReturnHandler>();
        services.AddCommandHandler<UpdateSupplierReturnCommand, string, UpdateSupplierReturnHandler>();
        services.AddCommandHandler<SubmitSupplierReturnCommand, string, SubmitSupplierReturnHandler>();
        services.AddCommandHandler<ApproveSupplierReturnCommand, SupplierReturnPostedResponse, ApproveSupplierReturnHandler>();
        services.AddCommandHandler<RejectSupplierReturnCommand, string, RejectSupplierReturnHandler>();
        services.AddCommandHandler<CancelSupplierReturnCommand, string, CancelSupplierReturnHandler>();
        services.AddQueryHandler<GetSupplierReturnQuery, SupplierReturnDetailsResponse, GetSupplierReturnHandler>();
        services.AddQueryHandler<ListSupplierReturnsQuery, PagedResult<SupplierReturnSummaryResponse>, ListSupplierReturnsHandler>();

        services.AddScoped<PurchasingReferences>();
        services.AddScoped<DocumentNumbering>();
        services.AddScoped<IdempotentRequests>();
        services.AddScoped<InvoicePosting>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IDocumentPolicyRepository, DocumentPolicyRepository>();
        services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<ISupplierInvoiceRepository, SupplierInvoiceRepository>();
        services.AddScoped<ISupplierReturnRepository, SupplierReturnRepository>();
        services.AddScoped<IPurchasingLocks, PurchasingLocks>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<ReferenceNames>();
        services.AddScoped<ISupplierReadStore, SupplierReadStore>();
        services.AddScoped<IPurchaseRequestReadStore, PurchaseRequestReadStore>();
        services.AddScoped<IPurchaseOrderReadStore, PurchaseOrderReadStore>();
        services.AddScoped<IGoodsReceiptReadStore, GoodsReceiptReadStore>();
        services.AddScoped<ISupplierInvoiceReadStore, SupplierInvoiceReadStore>();
        services.AddScoped<ISupplierReturnReadStore, SupplierReturnReadStore>();

        services.AddPermissions("Purchasing", PurchasingPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapPurchasingEndpoints(this IEndpointRouteBuilder app) =>
        app.MapSupplierEndpoints()
            .MapPurchasingPolicyEndpoints()
            .MapPurchaseRequestEndpoints()
            .MapPurchaseOrderEndpoints()
            .MapGoodsReceiptEndpoints()
            .MapSupplierInvoiceEndpoints()
            .MapSupplierReturnEndpoints();
}
