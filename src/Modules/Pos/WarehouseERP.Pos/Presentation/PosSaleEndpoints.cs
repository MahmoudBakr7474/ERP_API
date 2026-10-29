using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Features.Sales.CheckoutPosSale;
using WarehouseERP.Pos.Application.Features.Sales.GetPosSale;
using WarehouseERP.Pos.Application.Features.Sales.ListPosSales;
using WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;
using WarehouseERP.Pos.Application.Features.Sales.ReprintPosSale;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Presentation.Contracts.Sales;

namespace WarehouseERP.Pos.Presentation;

// Quoting and checking out need pos.sell (checkout with an Idempotency-Key, on the caller's own open shift); the sales
// history pos.read; printing a copy pos.receipts.reprint. The terminal's warehouse must be in the user's scope.
internal static class PosSaleEndpoints
{
    public const string Route = "/api/pos/sales";

    public static IEndpointRouteBuilder MapPosSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosSales");

        group.MapPost("/quote", async (QuotePosCartRequest request, IQueryHandler<QuotePosCartQuery, PosCartQuoteResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new QuotePosCartQuery(request.TerminalId, request.Lines), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("QuotePosCart")
            .Produces<PosCartQuoteResponse>()
            .CommandProblems();

        group.MapPost("/", CheckoutAsync)
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("CheckoutPosSale")
            .Produces<PosSaleResponse>()
            .CommandProblems();

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PosPermissions.Read)
            .WithName("ListPosSales")
            .Produces<PagedResult<PosSaleSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPosSaleQuery, PosSaleResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPosSaleQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Read)
            .WithName("GetPosSale")
            .Produces<PosSaleResponse>()
            .QueryProblems();

        group.MapPost("/{id:guid}/reprint", async (Guid id, ICommandHandler<ReprintPosSaleCommand, PosSaleResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ReprintPosSaleCommand(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.ReceiptsReprint)
            .WithName("ReprintPosSale")
            .Produces<PosSaleResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> CheckoutAsync(
        CheckoutPosSaleRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<CheckoutPosSaleCommand, PosSaleResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new CheckoutPosSaleCommand(
            request.TerminalId, request.CustomerId, request.CustomerName, request.CustomerPhone, request.Notes, request.Lines,
            request.Payments, idempotencyKey ?? Guid.Empty), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPosSalesQuery, PagedResult<PosSaleSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? terminalId = null, Guid? shiftId = null, Guid? warehouseId = null, Guid? customerId = null,
        Guid? cashierUserId = null, string? search = null, DateOnly? from = null, DateOnly? to = null) =>
        (await handler.HandleAsync(new ListPosSalesQuery(page, pageSize, terminalId, shiftId, warehouseId, customerId, cashierUserId, search,
            from, to), ct)).ToHttpResult(TypedResults.Ok);
}
