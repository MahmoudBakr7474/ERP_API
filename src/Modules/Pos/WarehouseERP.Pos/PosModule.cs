using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Contracts.CardIncidents;
using WarehouseERP.Pos.Application.Contracts.Items;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Contracts.Terminals;
using WarehouseERP.Pos.Application.Features.CardIncidents.ListPosCardIncidents;
using WarehouseERP.Pos.Application.Features.CardIncidents.ResolvePosCardIncident;
using WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;
using WarehouseERP.Pos.Application.Features.Items;
using WarehouseERP.Pos.Application.Features.Items.LookupPosItem;
using WarehouseERP.Pos.Application.Features.Items.SearchPosItems;
using WarehouseERP.Pos.Application.Features.Returns.CreatePosReturn;
using WarehouseERP.Pos.Application.Features.Returns.GetPosReturn;
using WarehouseERP.Pos.Application.Features.Returns.ListPosReturns;
using WarehouseERP.Pos.Application.Features.Returns.ReprintPosReturn;
using WarehouseERP.Pos.Application.Features.Sales;
using WarehouseERP.Pos.Application.Features.Sales.CheckoutPosSale;
using WarehouseERP.Pos.Application.Features.Sales.GetPosSale;
using WarehouseERP.Pos.Application.Features.Sales.ListPosSales;
using WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;
using WarehouseERP.Pos.Application.Features.Sales.ReprintPosSale;
using WarehouseERP.Pos.Application.Features.Settings.GetPosSettings;
using WarehouseERP.Pos.Application.Features.Settings.UpdatePosSettings;
using WarehouseERP.Pos.Application.Features.Shifts.ClosePosShift;
using WarehouseERP.Pos.Application.Features.Shifts.GetCurrentPosShift;
using WarehouseERP.Pos.Application.Features.Shifts.GetPosShift;
using WarehouseERP.Pos.Application.Features.Shifts.ListPosShifts;
using WarehouseERP.Pos.Application.Features.Shifts.OpenPosShift;
using WarehouseERP.Pos.Application.Features.Shifts.RecordPosCashMovement;
using WarehouseERP.Pos.Application.Features.Terminals.CreatePosTerminal;
using WarehouseERP.Pos.Application.Features.Terminals.GetPosTerminal;
using WarehouseERP.Pos.Application.Features.Terminals.ListPosTerminals;
using WarehouseERP.Pos.Application.Features.Terminals.SetPosTerminalActive;
using WarehouseERP.Pos.Application.Features.Terminals.UpdatePosTerminal;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Infrastructure.Persistence;
using WarehouseERP.Pos.Infrastructure.ReadModels;
using WarehouseERP.Pos.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos;

// Tills: terminals, cashier shifts and their drawers, checkout, receipts, returns and refunds (docs/modules/POS and ADR
// 0019). Prices, VAT and customers come from Sales (ISalesPricing, ICustomerDirectory), products from Catalog, warehouses,
// locations, branches and the company from Organization, display names from Identity; stock moves only through Inventory's
// IPosStockPosting, from unreserved stock. The host calls AddPosModule and MapPosEndpoints; ITransactionRunner comes from
// Persistence.
public static class PosModule
{
    public static IServiceCollection AddPosModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CheckoutPosSaleValidator>(includeInternalTypes: true);

        services.AddQueryHandler<GetPosSettingsQuery, PosSettingsResponse, GetPosSettingsHandler>();
        services.AddCommandHandler<UpdatePosSettingsCommand, string, UpdatePosSettingsHandler>();

        services.AddCommandHandler<CreatePosTerminalCommand, Guid, CreatePosTerminalHandler>();
        services.AddCommandHandler<UpdatePosTerminalCommand, string, UpdatePosTerminalHandler>();
        services.AddCommandHandler<SetPosTerminalActiveCommand, Done, SetPosTerminalActiveHandler>();
        services.AddQueryHandler<GetPosTerminalQuery, PosTerminalResponse, GetPosTerminalHandler>();
        services.AddQueryHandler<ListPosTerminalsQuery, PagedResult<PosTerminalResponse>, ListPosTerminalsHandler>();

        services.AddCommandHandler<OpenPosShiftCommand, Guid, OpenPosShiftHandler>();
        services.AddCommandHandler<RecordPosCashMovementCommand, string, RecordPosCashMovementHandler>();
        services.AddCommandHandler<ClosePosShiftCommand, PosShiftResponse, ClosePosShiftHandler>();
        services.AddQueryHandler<GetCurrentPosShiftQuery, PosShiftResponse, GetCurrentPosShiftHandler>();
        services.AddQueryHandler<GetPosShiftQuery, PosShiftResponse, GetPosShiftHandler>();
        services.AddQueryHandler<ListPosShiftsQuery, PagedResult<PosShiftSummaryResponse>, ListPosShiftsHandler>();

        services.AddQueryHandler<LookupPosItemQuery, PosItemResponse, LookupPosItemHandler>();
        services.AddQueryHandler<SearchPosItemsQuery, IReadOnlyList<PosItemResponse>, SearchPosItemsHandler>();
        services.AddQueryHandler<SearchPosCustomersQuery, IReadOnlyList<PosCustomerResponse>, SearchPosCustomersHandler>();

        services.AddQueryHandler<QuotePosCartQuery, PosCartQuoteResponse, QuotePosCartHandler>();
        services.AddCommandHandler<CheckoutPosSaleCommand, PosSaleResponse, CheckoutPosSaleHandler>();
        services.AddQueryHandler<GetPosSaleQuery, PosSaleResponse, GetPosSaleHandler>();
        services.AddQueryHandler<ListPosSalesQuery, PagedResult<PosSaleSummaryResponse>, ListPosSalesHandler>();
        services.AddCommandHandler<ReprintPosSaleCommand, PosSaleResponse, ReprintPosSaleHandler>();

        services.AddCommandHandler<CreatePosReturnCommand, PosReturnResponse, CreatePosReturnHandler>();
        services.AddQueryHandler<GetPosReturnQuery, PosReturnResponse, GetPosReturnHandler>();
        services.AddQueryHandler<ListPosReturnsQuery, PagedResult<PosReturnSummaryResponse>, ListPosReturnsHandler>();
        services.AddCommandHandler<ReprintPosReturnCommand, PosReturnResponse, ReprintPosReturnHandler>();

        services.AddQueryHandler<ListPosCardIncidentsQuery, PagedResult<PosCardIncidentResponse>, ListPosCardIncidentsHandler>();
        services.AddCommandHandler<ResolvePosCardIncidentCommand, string, ResolvePosCardIncidentHandler>();

        services.AddScoped<PosReferences>();
        services.AddScoped<TillAccess>();
        services.AddScoped<DocumentNumbering>();
        services.AddScoped<IdempotentRequests>();
        services.AddScoped<CartPricing>();
        services.AddScoped<PosItems>();
        services.AddScoped<IPosSettingsRepository, PosSettingsRepository>();
        services.AddScoped<IPosTerminalRepository, PosTerminalRepository>();
        services.AddScoped<IPosShiftRepository, PosShiftRepository>();
        services.AddScoped<IPosSaleRepository, PosSaleRepository>();
        services.AddScoped<IPosReturnRepository, PosReturnRepository>();
        services.AddScoped<ICardIncidentRepository, CardIncidentRepository>();
        services.AddScoped<IPosLocks, PosLocks>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<PosNames>();
        services.AddScoped<IPosTerminalReadStore, PosTerminalReadStore>();
        services.AddScoped<IPosShiftReadStore, PosShiftReadStore>();
        services.AddScoped<IPosSaleReadStore, PosSaleReadStore>();
        services.AddScoped<IPosReturnReadStore, PosReturnReadStore>();
        services.AddScoped<ICardIncidentReadStore, CardIncidentReadStore>();

        services.AddPermissions("Pos", PosPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapPosEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPosSettingsEndpoints()
            .MapPosTerminalEndpoints()
            .MapPosItemEndpoints()
            .MapPosCustomerEndpoints()
            .MapPosShiftEndpoints()
            .MapPosSaleEndpoints()
            .MapPosReturnEndpoints()
            .MapPosCardIncidentEndpoints();
}
