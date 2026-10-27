using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogTesting;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class CategoryApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static async Task<string> RowVersionAsync(HttpClient client, Guid id) =>
        (await client.GetJsonAsync($"{Categories}/{id}")).GetProperty("rowVersion").GetString()!;

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, Guid id, Guid? parentId, string rowVersion) =>
        client.PutJsonAsync($"{Categories}/{id}", new { nameAr = "تصنيف", nameEn = (string?)null, parentId, rowVersion });

    [Fact]
    public async Task Create_a_tree_rename_and_move_a_category()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var food = await CreateCategoryAsync(client);
        var drinks = await CreateCategoryAsync(client, food);
        var other = await CreateCategoryAsync(client);

        var details = await client.GetJsonAsync($"{Categories}/{drinks}");
        Assert.Equal(food, details.GetProperty("parentId").GetGuid());
        var code = details.GetProperty("code").GetString()!;
        await (await client.PostJsonAsync(Categories, new { code, nameAr = "مكرر", parentId = (Guid?)null }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Category.CodeTaken");

        var renamed = await client.PutJsonAsync($"{Categories}/{drinks}",
            new { nameAr = "مشروبات", nameEn = "Drinks", parentId = other, rowVersion = details.GetProperty("rowVersion").GetString() });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        await (await MoveAsync(client, drinks, food, details.GetProperty("rowVersion").GetString()!))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        var all = await client.GetJsonAsync(Categories);
        var row = Assert.Single(all.EnumerateArray(), x => x.GetProperty("id").GetGuid() == drinks);
        Assert.Equal((other, "مشروبات", code), (row.GetProperty("parentId").GetGuid(), row.GetProperty("nameAr").GetString(),
            row.GetProperty("code").GetString()));

        // Back to the top level.
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(client, drinks, null, await RowVersionAsync(client, drinks))).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await client.GetJsonAsync($"{Categories}/{drinks}")).GetProperty("parentId").ValueKind);
    }

    [Fact]
    public async Task A_category_cant_move_under_itself_or_below_itself()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var a = await CreateCategoryAsync(client);
        var b = await CreateCategoryAsync(client, a);
        var c = await CreateCategoryAsync(client, b);

        await (await MoveAsync(client, a, c, await RowVersionAsync(client, a)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.Cycle");
        await (await MoveAsync(client, a, a, await RowVersionAsync(client, a)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.Cycle");
        await (await MoveAsync(client, b, b, await RowVersionAsync(client, b)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.Cycle");

        // Moving a subtree sideways is fine: C goes up under A.
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(client, c, a, await RowVersionAsync(client, c))).StatusCode);
    }

    [Fact]
    public async Task An_inactive_category_never_holds_an_active_one()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var parent = await CreateCategoryAsync(client);
        var child = await CreateCategoryAsync(client, parent);
        var elsewhere = await CreateCategoryAsync(client);

        await (await client.PostEmptyAsync($"{Categories}/{parent}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.HasActiveChildren");
        await client.PostEmptyAsync($"{Categories}/{child}/deactivate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Categories}/{parent}/deactivate").AssertNoContentAsync();

        await (await client.PostEmptyAsync($"{Categories}/{child}/activate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.ParentNotActive");
        await (await client.PostJsonAsync(Categories, new { code = DatabaseFixture.Code("CAT"), nameAr = "جديد", parentId = parent }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.ParentNotActive");
        await (await MoveAsync(client, elsewhere, parent, await RowVersionAsync(client, elsewhere)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.ParentNotActive");

        var inactive = await client.GetJsonAsync($"{Categories}?isActive=false");
        Assert.Contains(inactive.EnumerateArray(), x => x.GetProperty("id").GetGuid() == parent);
        Assert.DoesNotContain(inactive.EnumerateArray(), x => x.GetProperty("id").GetGuid() == elsewhere);

        await client.PostEmptyAsync($"{Categories}/{parent}/activate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Categories}/{child}/activate").AssertNoContentAsync();
    }

    [Fact]
    public async Task Products_stay_in_a_retired_category_but_none_are_newly_put_there()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var unit = await CreateUnitAsync(client);
        var category = await CreateCategoryAsync(client);
        var kept = await CreateProductAsync(client, unit, category);
        var loose = await CreateProductAsync(client, unit);
        await client.PostEmptyAsync($"{Categories}/{category}/deactivate").AssertNoContentAsync();

        var product = await client.GetJsonAsync($"{Products}/{kept}");
        Assert.Equal(category, product.GetProperty("categoryId").GetGuid());
        var renamed = await client.PutJsonAsync($"{Products}/{kept}",
            new { nameAr = "اسم جديد", categoryId = category, rowVersion = product.GetProperty("rowVersion").GetString() });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        var looseRow = await client.GetJsonAsync($"{Products}/{loose}");
        await (await client.PutJsonAsync($"{Products}/{loose}",
                new { nameAr = "صنف", categoryId = category, rowVersion = looseRow.GetProperty("rowVersion").GetString() }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.NotActive");
        await (await client.PostJsonAsync(Products, new
            {
                sku = DatabaseFixture.Code("SKU"), nameAr = "صنف", baseUnitId = unit, categoryId = category,
                type = ProductType.StockItem, trackingMode = TrackingMode.None,
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Category.NotActive");
    }

    [Fact]
    public async Task Categories_are_read_with_product_read_and_changed_only_with_categories_manage()
    {
        var reader = _api.CreateClient().As(CatalogPermissions.ProductsRead);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync(Categories)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await reader.PostJsonAsync(Categories, new { code = DatabaseFixture.Code("CAT"), nameAr = "تصنيف" })).StatusCode);
        await (await _api.CreateClient().As(AllPermissions).GetAsync($"{Categories}/{Guid.NewGuid()}"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Category.NotFound");
    }
}
