using System.Text.Json.Serialization;
using WarehouseERP.Accounting;
using WarehouseERP.Api.ApiDocumentation;
using WarehouseERP.Api.ErrorHandling;
using WarehouseERP.Api.Observability;
using WarehouseERP.Api.Security;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog;
using WarehouseERP.Identity;
using WarehouseERP.Inventory;
using WarehouseERP.Organization;
using WarehouseERP.Persistence;
using WarehouseERP.Pos;
using WarehouseERP.Purchasing;
using WarehouseERP.Sales;

var builder = WebApplication.CreateBuilder(args);

builder.AddErpObservability();
builder.Services
    .AddErpPersistence()
    .AddErpProblemDetails()
    .AddErpRateLimiting()
    .AddErpReverseProxy()
    .AddIdentityModule() // authentication, permission policies and ICurrentUser for every module
    .AddOrganizationModule()
    .AddCatalogModule()
    .AddInventoryModule()
    .AddPurchasingModule()
    .AddSalesModule()
    .AddPosModule()
    .AddAccountingModule();

builder.Services.AddOptions<PaginationOptions>()
    .BindConfiguration(PaginationOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(o => o.DefaultPageSize <= o.MaxPageSize, "Pagination:DefaultPageSize must not exceed MaxPageSize.")
    .ValidateOnStart();

// Enums travel as names ("Physical", "Batch"), which stay stable for the React client; numbers only as JSON numbers,
// so the OpenAPI contract (and the generated client) says "integer", not "integer or numeric string".
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddErpOpenApi();

var app = builder.Build();

app.UseErpForwardedHeaders(); // first: the client IP and https scheme behind a trusted TLS-terminating proxy
if (!app.Environment.IsDevelopment())
    app.UseHsts(); // browsers that saw the site over https never fall back to http
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseErpRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter(); // before authentication: a refused request costs no session lookup
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
app.MapInventoryEndpoints();
app.MapPurchasingEndpoints();
app.MapSalesEndpoints();
app.MapPosEndpoints();
app.MapAccountingEndpoints();

app.Run();

// Lets integration tests host the API with WebApplicationFactory<Program>.
public partial class Program;
