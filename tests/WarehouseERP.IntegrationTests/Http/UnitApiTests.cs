using System.Net;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogTesting;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class UnitApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Create_read_update_and_list_a_unit()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var code = DatabaseFixture.Code("KG");
        var id = await client.PostJsonAsync(Units, new { code, nameAr = "كيلو", nameEn = (string?)null, allowsFraction = false })
            .CreatedIdAsync();

        var unit = await client.GetJsonAsync($"{Units}/{id}");
        Assert.Equal((code, "كيلو", false, true), (unit.GetProperty("code").GetString(), unit.GetProperty("nameAr").GetString(),
            unit.GetProperty("allowsFraction").GetBoolean(), unit.GetProperty("isActive").GetBoolean()));
        var original = unit.GetProperty("rowVersion").GetString();

        // Codes are unique whatever their case.
        await (await client.PostJsonAsync(Units, new { code = code.ToLowerInvariant(), nameAr = "مكرر", allowsFraction = false }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Unit.CodeTaken");

        var updated = await client.PutJsonAsync($"{Units}/{id}",
            new { nameAr = "كيلوجرام", nameEn = "Kilogram", allowsFraction = true, rowVersion = original });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        await (await client.PutJsonAsync($"{Units}/{id}", new { nameAr = "قديم", allowsFraction = false, rowVersion = original }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        unit = await client.GetJsonAsync($"{Units}/{id}");
        Assert.Equal((code, "Kilogram", true), (unit.GetProperty("code").GetString(), unit.GetProperty("nameEn").GetString(),
            unit.GetProperty("allowsFraction").GetBoolean()));

        var page = await client.GetJsonAsync($"{Units}?search={code}&isActive=true");
        Assert.Equal(id, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        page = await client.GetJsonAsync($"{Units}?search={code}&isActive=false");
        Assert.Empty(page.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task A_unit_used_by_an_active_product_cant_be_deactivated()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(client);
        var carton = await CreateUnitAsync(client);
        var product = await CreateProductAsync(client, piece);
        var productCarton = await AddUnitAsync(client, product, carton, 12m);

        await (await client.PostEmptyAsync($"{Units}/{piece}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Unit.InUse");
        await (await client.PostEmptyAsync($"{Units}/{carton}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Unit.InUse");

        // Once the product unit is retired the carton is free; the base unit is free only once the product is.
        await client.PostEmptyAsync($"{Products}/{product}/units/{productCarton}/deactivate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Units}/{carton}/deactivate").AssertNoContentAsync();
        await (await client.PostEmptyAsync($"{Products}/{product}/units/{productCarton}/activate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Unit.NotActive");

        await client.PostEmptyAsync($"{Products}/{product}/deactivate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Units}/{piece}/deactivate").AssertNoContentAsync();
        await (await client.PostEmptyAsync($"{Products}/{product}/activate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Unit.NotActive");

        await client.PostEmptyAsync($"{Units}/{piece}/activate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Products}/{product}/activate").AssertNoContentAsync();
    }

    [Fact]
    public async Task Units_are_read_with_product_read_and_changed_only_with_units_manage()
    {
        var reader = _api.CreateClient().As(CatalogPermissions.ProductsRead);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync(Units)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await reader.PostJsonAsync(Units, new { code = DatabaseFixture.Code("U"), nameAr = "قطعة", allowsFraction = false })).StatusCode);

        var id = await CreateUnitAsync(_api.CreateClient().As(AllPermissions));
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostEmptyAsync($"{Units}/{id}/deactivate")).StatusCode);
    }

    [Fact]
    public async Task Bad_input_and_unknown_units_are_answered_clearly()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var problem = await (await client.PostJsonAsync(Units, new { code = "قطعة", nameAr = "", allowsFraction = false }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal(2, problem.GetProperty("errors").GetArrayLength());

        await (await client.GetAsync($"{Units}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Unit.NotFound");
        await (await client.PostEmptyAsync($"{Units}/{Guid.NewGuid()}/activate"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Unit.NotFound");
    }
}
