using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.Organization.Application.Features.Geography.CreateCity;
using WarehouseERP.Organization.Application.Features.Geography.CreateCountry;
using WarehouseERP.Organization.Application.Features.Geography.CreateGovernorate;
using WarehouseERP.Organization.Application.Features.Geography.ListCities;
using WarehouseERP.Organization.Application.Features.Geography.ListCountries;
using WarehouseERP.Organization.Application.Features.Geography.ListGovernorates;
using WarehouseERP.Organization.Application.Features.Geography.SetCityActive;
using WarehouseERP.Organization.Application.Features.Geography.SetCountryActive;
using WarehouseERP.Organization.Application.Features.Geography.SetGovernorateActive;
using WarehouseERP.Organization.Application.Features.Geography.UpdateCity;
using WarehouseERP.Organization.Application.Features.Geography.UpdateCountry;
using WarehouseERP.Organization.Application.Features.Geography.UpdateGovernorate;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Contracts.Geography;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// The address lookups: country → governorate → city. Any signed-in user reads them (every address form needs them);
// changes need organization.geography.manage and company-wide access (403 otherwise).
internal static class GeographyEndpoints
{
    public const string CountriesRoute = "/api/organization/countries";
    public const string GovernoratesRoute = "/api/organization/governorates";
    public const string CitiesRoute = "/api/organization/cities";

    public static IEndpointRouteBuilder MapGeographyEndpoints(this IEndpointRouteBuilder app)
    {
        var countries = app.MapGroup(CountriesRoute).WithTags("Geography");
        countries.MapGet("/", ListCountriesAsync)
            .RequireAuthorization()
            .WithName("ListCountries")
            .Produces<IReadOnlyList<CountryResponse>>();
        countries.MapPost("/", CreateCountryAsync)
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("CreateCountry")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        countries.MapPut("/{id:guid}", (Guid id, RenameGeographyRequest request, ICommandHandler<UpdateCountryCommand, string> handler,
                CancellationToken ct) => UpdatedAsync(handler.HandleAsync(new UpdateCountryCommand(id, request.NameAr, request.NameEn, request.RowVersion), ct)))
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("UpdateCountry")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        MapActivation<SetCountryActiveCommand>(countries, "Country", (id, active) => new SetCountryActiveCommand(id, active));

        var governorates = app.MapGroup(GovernoratesRoute).WithTags("Geography");
        governorates.MapGet("/", ListGovernoratesAsync)
            .RequireAuthorization()
            .WithName("ListGovernorates")
            .Produces<IReadOnlyList<GovernorateResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        governorates.MapPost("/", CreateGovernorateAsync)
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("CreateGovernorate")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        governorates.MapPut("/{id:guid}", (Guid id, RenameGeographyRequest request, ICommandHandler<UpdateGovernorateCommand, string> handler,
                CancellationToken ct) => UpdatedAsync(handler.HandleAsync(new UpdateGovernorateCommand(id, request.NameAr, request.NameEn, request.RowVersion), ct)))
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("UpdateGovernorate")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        MapActivation<SetGovernorateActiveCommand>(governorates, "Governorate", (id, active) => new SetGovernorateActiveCommand(id, active));

        var cities = app.MapGroup(CitiesRoute).WithTags("Geography");
        cities.MapGet("/", ListCitiesAsync)
            .RequireAuthorization()
            .WithName("ListCities")
            .Produces<IReadOnlyList<CityResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        cities.MapPost("/", CreateCityAsync)
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("CreateCity")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        cities.MapPut("/{id:guid}", (Guid id, RenameGeographyRequest request, ICommandHandler<UpdateCityCommand, string> handler,
                CancellationToken ct) => UpdatedAsync(handler.HandleAsync(new UpdateCityCommand(id, request.NameAr, request.NameEn, request.RowVersion), ct)))
            .RequireAuthorization(OrganizationPermissions.GeographyManage)
            .WithName("UpdateCity")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        MapActivation<SetCityActiveCommand>(cities, "City", (id, active) => new SetCityActiveCommand(id, active));

        return app;
    }

    // POST /{id}/activate and /{id}/deactivate, answered with 204.
    private static void MapActivation<TCommand>(RouteGroupBuilder group, string name, Func<Guid, bool, TCommand> command)
    {
        foreach (var (path, active) in new[] { ("activate", true), ("deactivate", false) })
            group.MapPost($"/{{id:guid}}/{path}", async (Guid id, ICommandHandler<TCommand, Done> handler, CancellationToken ct) =>
                    (await handler.HandleAsync(command(id, active), ct)).ToHttpResult(_ => TypedResults.NoContent()))
                .RequireAuthorization(OrganizationPermissions.GeographyManage)
                .WithName((active ? "Activate" : "Deactivate") + name)
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListCountriesAsync(
        IQueryHandler<ListCountriesQuery, IReadOnlyList<CountryResponse>> handler, CancellationToken ct, bool? isActive = null) =>
        (await handler.HandleAsync(new ListCountriesQuery(isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListGovernoratesAsync(
        IQueryHandler<ListGovernoratesQuery, IReadOnlyList<GovernorateResponse>> handler, CancellationToken ct,
        string countryCode = "", bool? isActive = null) =>
        (await handler.HandleAsync(new ListGovernoratesQuery(countryCode, isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListCitiesAsync(
        IQueryHandler<ListCitiesQuery, IReadOnlyList<CityResponse>> handler, CancellationToken ct,
        Guid? governorateId = null, bool? isActive = null) =>
        (await handler.HandleAsync(new ListCitiesQuery(governorateId ?? Guid.Empty, isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateCountryAsync(
        CreateCountryCommand command, ICommandHandler<CreateCountryCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(id => TypedResults.Created($"{CountriesRoute}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> CreateGovernorateAsync(
        CreateGovernorateCommand command, ICommandHandler<CreateGovernorateCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(id => TypedResults.Created($"{GovernoratesRoute}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> CreateCityAsync(
        CreateCityCommand command, ICommandHandler<CreateCityCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(id => TypedResults.Created($"{CitiesRoute}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> UpdatedAsync(Task<Result<string>> pending) =>
        (await pending).ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
