using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Presentation;

// What every Purchasing command endpoint may answer besides success, and the mapping of a row-version result.
internal static class EndpointConventions
{
    /// <summary>A new GUID per posting or approval click, sent again unchanged when the client retries it.</summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    public static RouteHandlerBuilder CommandProblems(this RouteHandlerBuilder endpoint) =>
        endpoint.ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    public static RouteHandlerBuilder QueryProblems(this RouteHandlerBuilder endpoint) =>
        endpoint.ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> UpdatedAsync(Task<Result<string>> pending) =>
        (await pending).ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    public static async Task<IResult> CreatedAsync(Task<Result<Guid>> pending, string route) =>
        (await pending).ToHttpResult(id => TypedResults.Created($"{route}/{id}", new CreatedResponse(id)));
}
