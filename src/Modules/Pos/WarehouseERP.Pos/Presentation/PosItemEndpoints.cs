using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.Items;
using WarehouseERP.Pos.Application.Features.Items.LookupPosItem;
using WarehouseERP.Pos.Application.Features.Items.SearchPosItems;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Presentation;

// What a till scans and searches (pos.sell): items priced and stocked for that terminal.
internal static class PosItemEndpoints
{
    public const string Route = "/api/pos/terminals/{terminalId:guid}/items";

    public static IEndpointRouteBuilder MapPosItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosItems");

        group.MapGet("/lookup", async (Guid terminalId, IQueryHandler<LookupPosItemQuery, PosItemResponse> handler, CancellationToken ct,
                    string barcode = "") =>
                (await handler.HandleAsync(new LookupPosItemQuery(terminalId, barcode), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("LookupPosItem")
            .Produces<PosItemResponse>()
            .QueryProblems()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", async (Guid terminalId, IQueryHandler<SearchPosItemsQuery, IReadOnlyList<PosItemResponse>> handler,
                    CancellationToken ct, string search = "", int? take = null) =>
                (await handler.HandleAsync(new SearchPosItemsQuery(terminalId, search, take), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("SearchPosItems")
            .Produces<IReadOnlyList<PosItemResponse>>()
            .QueryProblems()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
