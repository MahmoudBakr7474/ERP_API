using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ApproveSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CancelSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CreateSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.GetSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ListSupplierInvoices;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.PostSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.RejectSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.SubmitSupplierInvoice;
using WarehouseERP.Purchasing.Application.Features.SupplierInvoices.UpdateSupplierInvoice;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.Documents;
using WarehouseERP.Purchasing.Presentation.Contracts.SupplierInvoices;

namespace WarehouseERP.Purchasing.Presentation;

// Entering, submitting, posting and cancelling need purchasing.invoices.manage; approving price differences and rejecting
// purchasing.documents.approve (whoever entered the invoice may approve it). Post and approve carry an Idempotency-Key.
internal static class SupplierInvoiceEndpoints
{
    public const string Route = "/api/purchasing/invoices";

    public static IEndpointRouteBuilder MapSupplierInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SupplierInvoices");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListSupplierInvoices")
            .Produces<PagedResult<SupplierInvoiceSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSupplierInvoiceQuery, SupplierInvoiceDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSupplierInvoiceQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("GetSupplierInvoice")
            .Produces<SupplierInvoiceDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSupplierInvoiceCommand command, ICommandHandler<CreateSupplierInvoiceCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PurchasingPermissions.InvoicesManage)
            .WithName("CreateSupplierInvoice")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSupplierInvoiceRequest request, ICommandHandler<UpdateSupplierInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSupplierInvoiceCommand(
                    id, request.SupplierInvoiceNumber, request.InvoiceDate, request.DueDate, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.InvoicesManage)
            .WithName("UpdateSupplierInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, PurchasingActionRequest request, ICommandHandler<SubmitSupplierInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitSupplierInvoiceCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.InvoicesManage)
            .WithName("SubmitSupplierInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/post", PostAsync)
            .RequireAuthorization(PurchasingPermissions.InvoicesManage)
            .WithName("PostSupplierInvoice")
            .Produces<SupplierInvoicePostedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("ApproveSupplierInvoice")
            .Produces<SupplierInvoicePostedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, PurchasingReasonRequest request, ICommandHandler<RejectSupplierInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectSupplierInvoiceCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("RejectSupplierInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, PurchasingActionRequest request, ICommandHandler<CancelSupplierInvoiceCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSupplierInvoiceCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.InvoicesManage)
            .WithName("CancelSupplierInvoice")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSupplierInvoicesQuery, PagedResult<SupplierInvoiceSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? supplierId = null, Guid? purchaseOrderId = null, SupplierInvoiceStatus? status = null,
        string? search = null, DateOnly? dueOnOrBefore = null) =>
        (await handler.HandleAsync(new ListSupplierInvoicesQuery(page, pageSize, supplierId, purchaseOrderId, status, search, dueOnOrBefore), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> PostAsync(
        Guid id, PurchasingActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<PostSupplierInvoiceCommand, SupplierInvoicePostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new PostSupplierInvoiceCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, PurchasingActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveSupplierInvoiceCommand, SupplierInvoicePostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveSupplierInvoiceCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
