using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Presentation;

// A cashier (pos.sell) finds an active registered customer to name on a sale; no Sales permission needed.
internal static class PosCustomerEndpoints
{
    public const string Route = "/api/pos/customers";

    public static IEndpointRouteBuilder MapPosCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup(Route).WithTags("PosCustomers")
            .MapGet("/", async (IQueryHandler<SearchPosCustomersQuery, IReadOnlyList<PosCustomerResponse>> handler, CancellationToken ct,
                    string search = "", int? take = null) =>
                (await handler.HandleAsync(new SearchPosCustomersQuery(search, take), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("SearchPosCustomers")
            .Produces<IReadOnlyList<PosCustomerResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return app;
    }
}
