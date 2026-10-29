using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Features.Invoices.CancelSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.CreateSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.GetSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.ListSalesInvoices;
using WarehouseERP.Sales.Application.Features.Invoices.PostSalesInvoice;
using WarehouseERP.Sales.Application.Features.Invoices.UpdateSalesInvoice;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Documents;
using WarehouseERP.Sales.Presentation.Contracts.Invoices;

namespace WarehouseERP.Sales.Presentation;

// Drafting, posting and cancelling invoices need sales.invoices.manage; posting takes an Idempotency-Key. The order's
// warehouse must be in the user's scope (checked by the handlers).
internal static class SalesInvoiceEndpoints
{
    public const string Route = "/api/sales/invoices";

    public static IEndpointRouteBuilder MapSalesInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesInvoices");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesInvoices")
            .Produces<PagedResult<SalesInvoiceSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSalesInvoiceQuery, SalesInvoiceDetailsResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesInvoiceQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesInvoice")
            .Produces<SalesInvoiceDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSalesInvoiceCommand command, ICommandHandler<CreateSalesInvoiceCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.InvoicesManage)
            .WithName("CreateSalesInvoice")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSalesInvoiceRequest request, ICommandHandler<UpdateSalesInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSalesInvoiceCommand(
                    id, request.InvoiceDate, request.CustomerReference, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.InvoicesManage)
            .WithName("UpdateSalesInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, SalesActionRequest request, ICommandHandler<CancelSalesInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSalesInvoiceCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.InvoicesManage)
            .WithName("CancelSalesInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/post", PostAsync)
            .RequireAuthorization(SalesPermissions.InvoicesManage)
            .WithName("PostSalesInvoice")
            .Produces<SalesInvoicePostedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesInvoicesQuery, PagedResult<SalesInvoiceSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? orderId = null, Guid? customerId = null,
        SalesInvoiceStatus? status = null, string? search = null) =>
        (await handler.HandleAsync(new ListSalesInvoicesQuery(page, pageSize, warehouseId, orderId, customerId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> PostAsync(
        Guid id, SalesActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<PostSalesInvoiceCommand, SalesInvoicePostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new PostSalesInvoiceCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
