using System.Text.Json.Serialization;
using WarehouseERP.Api.ErrorHandling;
using WarehouseERP.Api.Observability;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog;
using WarehouseERP.Identity;
using WarehouseERP.Organization;
using WarehouseERP.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddErpObservability();
builder.Services
    .AddErpPersistence()
    .AddErpProblemDetails()
    .AddIdentityModule() // authentication, permission policies and ICurrentUser for every module
    .AddOrganizationModule()
    .AddCatalogModule();

builder.Services.AddOptions<PaginationOptions>()
    .BindConfiguration(PaginationOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(o => o.DefaultPageSize <= o.MaxPageSize, "Pagination:DefaultPageSize must not exceed MaxPageSize.")
    .ValidateOnStart();

// Enums travel as names ("Physical", "Batch"), which stay stable for the React client.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseErpRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseIdentityModule(); // anti-forgery for cookie sessions, before any endpoint runs
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapErpHealthChecks();
app.MapIdentityEndpoints();
app.MapOrganizationEndpoints();
app.MapCatalogEndpoints();

app.Run();

// Lets integration tests host the API with WebApplicationFactory<Program>.
public partial class Program;
