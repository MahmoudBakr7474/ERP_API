using WarehouseERP.Catalog;
using WarehouseERP.Organization;
using WarehouseERP.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddErpPersistence(builder.Configuration.GetConnectionString("Erp")
        ?? throw new InvalidOperationException("ConnectionStrings:Erp is not set."))
    .AddOrganizationModule()
    .AddCatalogModule();

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
