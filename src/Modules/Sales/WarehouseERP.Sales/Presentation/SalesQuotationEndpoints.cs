using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Quotations;
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
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Documents;
using WarehouseERP.Sales.Presentation.Contracts.Quotations;

namespace WarehouseERP.Sales.Presentation;

// Drafting, submitting, declining, cancelling and converting need sales.quotations.manage; approving and rejecting
// sales.documents.approve (the creator may approve their own quotation). The quotation's warehouse must be in the user's
// scope (checked by the handlers).
internal static class SalesQuotationEndpoints
{
    public const string Route = "/api/sales/quotations";

    public static IEndpointRouteBuilder MapSalesQuotationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesQuotations");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesQuotations")
            .Produces<PagedResult<SalesQuotationSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSalesQuotationQuery, SalesQuotationDetailsResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesQuotationQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesQuotation")
            .Produces<SalesQuotationDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSalesQuotationCommand command, ICommandHandler<CreateSalesQuotationCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("CreateSalesQuotation")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSalesQuotationRequest request, ICommandHandler<UpdateSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSalesQuotationCommand(
                    id, request.CustomerId, request.QuotationDate, request.ValidUntil, request.CustomerReference, request.PaymentTermDays,
                    request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("UpdateSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, SalesActionRequest request, ICommandHandler<SubmitSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitSalesQuotationCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("SubmitSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", (Guid id, SalesActionRequest request, ICommandHandler<ApproveSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ApproveSalesQuotationCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("ApproveSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, SalesReasonRequest request, ICommandHandler<RejectSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectSalesQuotationCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("RejectSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/decline", (Guid id, SalesReasonRequest request, ICommandHandler<DeclineSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new DeclineSalesQuotationCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("DeclineSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, SalesReasonRequest request, ICommandHandler<CancelSalesQuotationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSalesQuotationCommand(id, request.Reason, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("CancelSalesQuotation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/convert", (Guid id, SalesActionRequest request, ICommandHandler<ConvertSalesQuotationCommand, Guid> handler,
                    CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(new ConvertSalesQuotationCommand(id, request.RowVersion), ct),
                    SalesOrderEndpoints.Route))
            .RequireAuthorization(SalesPermissions.QuotationsManage)
            .WithName("ConvertSalesQuotation")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesQuotationsQuery, PagedResult<SalesQuotationSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? customerId = null, SalesQuotationStatus? status = null,
        string? search = null) =>
        (await handler.HandleAsync(new ListSalesQuotationsQuery(page, pageSize, warehouseId, customerId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);
}
