using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Application.Features.Suppliers.CreateSupplier;
using WarehouseERP.Purchasing.Application.Features.Suppliers.FindSimilarSuppliers;
using WarehouseERP.Purchasing.Application.Features.Suppliers.GetSupplier;
using WarehouseERP.Purchasing.Application.Features.Suppliers.ListSuppliers;
using WarehouseERP.Purchasing.Application.Features.Suppliers.SetSupplierActive;
using WarehouseERP.Purchasing.Application.Features.Suppliers.UpdateSupplier;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.Suppliers;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Presentation;

// Suppliers are company-wide master data: reading needs purchasing.suppliers.read, changing purchasing.suppliers.manage.
internal static class SupplierEndpoints
{
    public const string Route = "/api/purchasing/suppliers";

    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Suppliers");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PurchasingPermissions.SuppliersRead)
            .WithName("ListSuppliers")
            .Produces<PagedResult<SupplierSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/similar", SimilarAsync)
            .RequireAuthorization(PurchasingPermissions.SuppliersRead)
            .WithName("FindSimilarSuppliers")
            .Produces<IReadOnlyList<SimilarSupplierResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(PurchasingPermissions.SuppliersRead)
            .WithName("GetSupplier")
            .Produces<SupplierDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", (CreateSupplierCommand command, ICommandHandler<CreateSupplierCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PurchasingPermissions.SuppliersManage)
            .WithName("CreateSupplier")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(PurchasingPermissions.SuppliersManage)
            .WithName("UpdateSupplier")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetSupplierActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetSupplierActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(PurchasingPermissions.SuppliersManage)
            .WithName("ActivateSupplier")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetSupplierActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetSupplierActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(PurchasingPermissions.SuppliersManage)
            .WithName("DeactivateSupplier")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSuppliersQuery, PagedResult<SupplierSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null, bool? isActive = null, string? countryCode = null) =>
        (await handler.HandleAsync(new ListSuppliersQuery(page, pageSize, search, isActive, countryCode), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> SimilarAsync(
        IQueryHandler<FindSimilarSuppliersQuery, IReadOnlyList<SimilarSupplierResponse>> handler, CancellationToken ct,
        string nameAr = "", string? nameEn = null, Guid? exceptSupplierId = null) =>
        (await handler.HandleAsync(new FindSimilarSuppliersQuery(nameAr, nameEn, exceptSupplierId), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(Guid id, IQueryHandler<GetSupplierQuery, SupplierDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetSupplierQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static Task<IResult> UpdateAsync(
        Guid id, UpdateSupplierRequest request, ICommandHandler<UpdateSupplierCommand, string> handler, CancellationToken ct) =>
        EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSupplierCommand(
            id, request.NameAr, request.NameEn, request.CountryCode, request.TaxRegistrationNumber, request.CommercialRegistrationNumber,
            request.CommercialRegistry, request.PaymentTermDays, request.Phone, request.Email, request.Notes, request.Contacts,
            request.Addresses, request.RowVersion), ct));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
