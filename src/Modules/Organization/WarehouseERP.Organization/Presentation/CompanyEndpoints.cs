using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Companies;
using WarehouseERP.Organization.Application.Features.Companies.GetCompany;
using WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;
using WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation;

// The one company of the installation: read by every signed-in user, set up once, then its settings change.
internal static class CompanyEndpoints
{
    public const string Route = "/api/organization/company";

    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Company");

        // Any signed-in user: screens and documents show the company's name, currency and time zone.
        group.MapGet("/", GetAsync)
            .RequireAuthorization()
            .WithName("GetCompany")
            .Produces<CompanyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", SetUpAsync)
            .RequireAuthorization(OrganizationPermissions.CompanyUpdate)
            .WithName("SetUpCompany")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/", UpdateAsync)
            .RequireAuthorization(OrganizationPermissions.CompanyUpdate)
            .WithName("UpdateCompany")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetAsync(IQueryHandler<GetCompanyQuery, CompanyResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetCompanyQuery(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> SetUpAsync(
        SetUpCompanyCommand command, ICommandHandler<SetUpCompanyCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(id => TypedResults.Created(Route, new CreatedResponse(id)));

    private static async Task<IResult> UpdateAsync(
        UpdateCompanyCommand command, ICommandHandler<UpdateCompanyCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
