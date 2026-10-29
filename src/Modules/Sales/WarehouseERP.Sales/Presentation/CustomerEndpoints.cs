using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Application.Features.Customers.CreateCustomer;
using WarehouseERP.Sales.Application.Features.Customers.FindSimilarCustomers;
using WarehouseERP.Sales.Application.Features.Customers.GetCustomer;
using WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;
using WarehouseERP.Sales.Application.Features.Customers.ListCustomers;
using WarehouseERP.Sales.Application.Features.Customers.SetCustomerActive;
using WarehouseERP.Sales.Application.Features.Customers.SetCustomerCredit;
using WarehouseERP.Sales.Application.Features.Customers.UpdateCustomer;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Customers;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Presentation;

// Customers are company-wide master data: reading needs sales.customers.read, changing sales.customers.manage, and credit
// terms and holds sales.credit.manage.
internal static class CustomerEndpoints
{
    public const string Route = "/api/sales/customers";

    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Customers");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.CustomersRead)
            .WithName("ListCustomers")
            .Produces<PagedResult<CustomerSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/similar", SimilarAsync)
            .RequireAuthorization(SalesPermissions.CustomersRead)
            .WithName("FindSimilarCustomers")
            .Produces<IReadOnlyList<SimilarCustomerResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetCustomerQuery, CustomerDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetCustomerQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.CustomersRead)
            .WithName("GetCustomer")
            .Produces<CustomerDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/exposure", async (Guid id, IQueryHandler<GetCustomerExposureQuery, CustomerExposureResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new GetCustomerExposureQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.CustomersRead)
            .WithName("GetCustomerExposure")
            .Produces<CustomerExposureResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateCustomerCommand command, ICommandHandler<CreateCustomerCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.CustomersManage)
            .WithName("CreateCustomer")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateCustomerRequest request, ICommandHandler<UpdateCustomerCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateCustomerCommand(
                    id, request.NameAr, request.NameEn, request.CountryCode, request.TaxRegistrationNumber,
                    request.CommercialRegistrationNumber, request.CommercialRegistry, request.Phone, request.Mobile, request.Email,
                    request.Notes, request.Contacts, request.Addresses, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.CustomersManage)
            .WithName("UpdateCustomer")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPut("/{id:guid}/credit", (Guid id, SetCustomerCreditRequest request, ICommandHandler<SetCustomerCreditCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SetCustomerCreditCommand(
                    id, request.PaymentTermDays, request.CreditLimit, request.IsOnCreditHold, request.CreditHoldReason, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.CreditManage)
            .WithName("SetCustomerCredit")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetCustomerActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetCustomerActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(SalesPermissions.CustomersManage)
            .WithName("ActivateCustomer")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetCustomerActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetCustomerActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(SalesPermissions.CustomersManage)
            .WithName("DeactivateCustomer")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null, bool? isActive = null, CustomerType? customerType = null,
        bool? isOnCreditHold = null, string? countryCode = null) =>
        (await handler.HandleAsync(new ListCustomersQuery(page, pageSize, search, isActive, customerType, isOnCreditHold, countryCode), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> SimilarAsync(
        IQueryHandler<FindSimilarCustomersQuery, IReadOnlyList<SimilarCustomerResponse>> handler, CancellationToken ct,
        string nameAr = "", string? nameEn = null, string? mobile = null, Guid? exceptCustomerId = null) =>
        (await handler.HandleAsync(new FindSimilarCustomersQuery(nameAr, nameEn, mobile, exceptCustomerId), ct)).ToHttpResult(TypedResults.Ok);
}
