using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Contracts.Orders;
using WarehouseERP.Sales.Application.Contracts.Prices;
using WarehouseERP.Sales.Application.Contracts.Quotations;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Features.Customers.CreateCustomer;
using WarehouseERP.Sales.Application.Features.Customers.FindSimilarCustomers;
using WarehouseERP.Sales.Application.Features.Customers.GetCustomer;
using WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;
using WarehouseERP.Sales.Application.Features.Customers.ListCustomers;
using WarehouseERP.Sales.Application.Features.Customers.SetCustomerActive;
using WarehouseERP.Sales.Application.Features.Customers.SetCustomerCredit;
using WarehouseERP.Sales.Application.Features.Customers.UpdateCustomer;
using WarehouseERP.Sales.Application.Features.Deliveries.CancelSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.CreateSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.GetSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.ListSalesDeliveries;
using WarehouseERP.Sales.Application.Features.Deliveries.PostSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.UpdateSalesDelivery;
using WarehouseERP.Sales.Application.Features.Invoices.CancelSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.CreateSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.GetSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.ListSalesInvoices;
using WarehouseERP.Sales.Application.Features.Invoices.PostSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.UpdateSalesInvoice;
using WarehouseERP.Sales.Application.Features.Orders;
using WarehouseERP.Sales.Application.Features.Orders.ApproveSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CancelSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CloseSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CreateSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.GetSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.ListSalesOrders;
using WarehouseERP.Sales.Application.Features.Orders.ReduceSalesOrderLine;
using WarehouseERP.Sales.Application.Features.Orders.RejectSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.SubmitSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.UpdateSalesOrder;
using WarehouseERP.Sales.Application.Features.Prices.ListSalesPrices;
using WarehouseERP.Sales.Application.Features.Prices.RemoveSalesPrice;
using WarehouseERP.Sales.Application.Features.Prices.SetSalesPrice;
using WarehouseERP.Sales.Application.Features.Quotations.ApproveSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.CancelSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.ConvertSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.CreateSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.DeclineSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.GetSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.ListSalesQuotations;
using WarehouseERP.Sales.Application.Features.Quotations.RejectSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.SubmitSalesQuotation;
using WarehouseERP.Sales.Application.Features.Quotations.UpdateSalesQuotation;
using WarehouseERP.Sales.Application.Features.Returns.ApproveSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.CancelSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.CreateSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.GetSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.ListSalesReturns;
using WarehouseERP.Sales.Application.Features.Returns.RejectSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.SubmitSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.UpdateSalesReturn;
using WarehouseERP.Sales.Application.Features.Settings.GetSalesSettings;
using WarehouseERP.Sales.Application.Features.Settings.UpdateSalesSettings;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Infrastructure.Persistence;
using WarehouseERP.Sales.Infrastructure.ReadModels;
using WarehouseERP.Sales.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales;

// Customers, the price list, quotations, orders, deliveries, invoices and customer returns (docs/modules/Sales and ADR
// 0018). Stock is reserved, delivered and taken back only through Inventory's IStockReservations and ISalesStockPosting;
// products and units come from Catalog, warehouses, locations, branches, the company and address lookups from Organization,
// display names from Identity, all through their Contracts. The host calls AddSalesModule and MapSalesEndpoints;
// ITransactionRunner comes from Persistence.
public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>(includeInternalTypes: true);

        services.AddCommandHandler<CreateCustomerCommand, Guid, CreateCustomerHandler>();
        services.AddCommandHandler<UpdateCustomerCommand, string, UpdateCustomerHandler>();
        services.AddCommandHandler<SetCustomerActiveCommand, Done, SetCustomerActiveHandler>();
        services.AddCommandHandler<SetCustomerCreditCommand, string, SetCustomerCreditHandler>();
        services.AddQueryHandler<GetCustomerQuery, CustomerDetailsResponse, GetCustomerHandler>();
        services.AddQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryResponse>, ListCustomersHandler>();
        services.AddQueryHandler<FindSimilarCustomersQuery, IReadOnlyList<SimilarCustomerResponse>, FindSimilarCustomersHandler>();
        services.AddQueryHandler<GetCustomerExposureQuery, CustomerExposureResponse, GetCustomerExposureHandler>();

        services.AddQueryHandler<GetSalesSettingsQuery, SalesSettingsResponse, GetSalesSettingsHandler>();
        services.AddCommandHandler<UpdateSalesSettingsCommand, string, UpdateSalesSettingsHandler>();

        services.AddQueryHandler<ListSalesPricesQuery, PagedResult<SalesPriceResponse>, ListSalesPricesHandler>();
        services.AddCommandHandler<SetSalesPriceCommand, Done, SetSalesPriceHandler>();
        services.AddCommandHandler<RemoveSalesPriceCommand, Done, RemoveSalesPriceHandler>();

        services.AddCommandHandler<CreateSalesQuotationCommand, Guid, CreateSalesQuotationHandler>();
        services.AddCommandHandler<UpdateSalesQuotationCommand, string, UpdateSalesQuotationHandler>();
        services.AddCommandHandler<SubmitSalesQuotationCommand, string, SubmitSalesQuotationHandler>();
        services.AddCommandHandler<ApproveSalesQuotationCommand, string, ApproveSalesQuotationHandler>();
        services.AddCommandHandler<RejectSalesQuotationCommand, string, RejectSalesQuotationHandler>();
        services.AddCommandHandler<DeclineSalesQuotationCommand, string, DeclineSalesQuotationHandler>();
        services.AddCommandHandler<CancelSalesQuotationCommand, string, CancelSalesQuotationHandler>();
        services.AddCommandHandler<ConvertSalesQuotationCommand, Guid, ConvertSalesQuotationHandler>();
        services.AddQueryHandler<GetSalesQuotationQuery, SalesQuotationDetailsResponse, GetSalesQuotationHandler>();
        services.AddQueryHandler<ListSalesQuotationsQuery, PagedResult<SalesQuotationSummaryResponse>, ListSalesQuotationsHandler>();

        services.AddCommandHandler<CreateSalesOrderCommand, Guid, CreateSalesOrderHandler>();
        services.AddCommandHandler<UpdateSalesOrderCommand, string, UpdateSalesOrderHandler>();
        services.AddCommandHandler<SubmitSalesOrderCommand, string, SubmitSalesOrderHandler>();
        services.AddCommandHandler<ApproveSalesOrderCommand, string, ApproveSalesOrderHandler>();
        services.AddCommandHandler<RejectSalesOrderCommand, string, RejectSalesOrderHandler>();
        services.AddCommandHandler<CancelSalesOrderCommand, string, CancelSalesOrderHandler>();
        services.AddCommandHandler<CloseSalesOrderCommand, string, CloseSalesOrderHandler>();
        services.AddCommandHandler<ReduceSalesOrderLineCommand, string, ReduceSalesOrderLineHandler>();
        services.AddQueryHandler<GetSalesOrderQuery, SalesOrderDetailsResponse, GetSalesOrderHandler>();
        services.AddQueryHandler<ListSalesOrdersQuery, PagedResult<SalesOrderSummaryResponse>, ListSalesOrdersHandler>();

        services.AddCommandHandler<CreateSalesDeliveryCommand, Guid, CreateSalesDeliveryHandler>();
        services.AddCommandHandler<UpdateSalesDeliveryCommand, string, UpdateSalesDeliveryHandler>();
        services.AddCommandHandler<CancelSalesDeliveryCommand, string, CancelSalesDeliveryHandler>();
        services.AddCommandHandler<PostSalesDeliveryCommand, SalesDeliveryPostedResponse, PostSalesDeliveryHandler>();
        services.AddQueryHandler<GetSalesDeliveryQuery, SalesDeliveryDetailsResponse, GetSalesDeliveryHandler>();
        services.AddQueryHandler<ListSalesDeliveriesQuery, PagedResult<SalesDeliverySummaryResponse>, ListSalesDeliveriesHandler>();

        services.AddCommandHandler<CreateSalesInvoiceCommand, Guid, CreateSalesInvoiceHandler>();
        services.AddCommandHandler<UpdateSalesInvoiceCommand, string, UpdateSalesInvoiceHandler>();
        services.AddCommandHandler<CancelSalesInvoiceCommand, string, CancelSalesInvoiceHandler>();
        services.AddCommandHandler<PostSalesInvoiceCommand, SalesInvoicePostedResponse, PostSalesInvoiceHandler>();
        services.AddQueryHandler<GetSalesInvoiceQuery, SalesInvoiceDetailsResponse, GetSalesInvoiceHandler>();
        services.AddQueryHandler<ListSalesInvoicesQuery, PagedResult<SalesInvoiceSummaryResponse>, ListSalesInvoicesHandler>();

        services.AddCommandHandler<CreateSalesReturnCommand, Guid, CreateSalesReturnHandler>();
        services.AddCommandHandler<UpdateSalesReturnCommand, string, UpdateSalesReturnHandler>();
        services.AddCommandHandler<SubmitSalesReturnCommand, string, SubmitSalesReturnHandler>();
        services.AddCommandHandler<ApproveSalesReturnCommand, SalesReturnApprovedResponse, ApproveSalesReturnHandler>();
        services.AddCommandHandler<RejectSalesReturnCommand, string, RejectSalesReturnHandler>();
        services.AddCommandHandler<CancelSalesReturnCommand, string, CancelSalesReturnHandler>();
        services.AddQueryHandler<GetSalesReturnQuery, SalesReturnDetailsResponse, GetSalesReturnHandler>();
        services.AddQueryHandler<ListSalesReturnsQuery, PagedResult<SalesReturnSummaryResponse>, ListSalesReturnsHandler>();

        services.AddScoped<SalesReferences>();
        services.AddScoped<LinePricing>();
        services.AddScoped<DocumentNumbering>();
        services.AddScoped<IdempotentRequests>();
        services.AddScoped<OrderConfirmation>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISalesSettingsRepository, SalesSettingsRepository>();
        services.AddScoped<ISalesPriceRepository, SalesPriceRepository>();
        services.AddScoped<ISalesQuotationRepository, SalesQuotationRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ISalesDeliveryRepository, SalesDeliveryRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<ISalesReturnRepository, SalesReturnRepository>();
        services.AddScoped<ISalesLocks, SalesLocks>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<ReferenceNames>();
        services.AddScoped<ICustomerReadStore, CustomerReadStore>();
        services.AddScoped<ISalesPriceReadStore, SalesPriceReadStore>();
        services.AddScoped<ISalesQuotationReadStore, SalesQuotationReadStore>();
        services.AddScoped<ISalesOrderReadStore, SalesOrderReadStore>();
        services.AddScoped<ISalesDeliveryReadStore, SalesDeliveryReadStore>();
        services.AddScoped<ISalesInvoiceReadStore, SalesInvoiceReadStore>();
        services.AddScoped<ISalesReturnReadStore, SalesReturnReadStore>();

        services.AddPermissions("Sales", SalesPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app) =>
        app.MapCustomerEndpoints()
            .MapSalesSettingsEndpoints()
            .MapSalesPriceEndpoints()
            .MapSalesQuotationEndpoints()
            .MapSalesOrderEndpoints()
            .MapSalesDeliveryEndpoints()
            .MapSalesInvoiceEndpoints()
            .MapSalesReturnEndpoints();
}
