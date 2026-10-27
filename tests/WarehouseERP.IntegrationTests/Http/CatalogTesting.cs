using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>Shared steps for the Catalog API tests: every value is unique, since the tests share one database.</summary>
internal static class CatalogTesting
{
    public const string Units = "/api/catalog/units";
    public const string Categories = "/api/catalog/categories";
    public const string Products = "/api/catalog/products";

    public const string AllPermissions =
        $"{CatalogPermissions.ProductsRead},{CatalogPermissions.ProductsCreate},{CatalogPermissions.ProductsUpdate}," +
        $"{CatalogPermissions.UnitsManage},{CatalogPermissions.CategoriesManage}";

    public static async Task<Guid> CreatedIdAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
    }

    public static Task<Guid> CreateUnitAsync(HttpClient client, bool allowsFraction = false) =>
        client.PostJsonAsync(Units, new { code = DatabaseFixture.Code("U"), nameAr = "قطعة", nameEn = "Piece", allowsFraction })
            .CreatedIdAsync();

    public static Task<Guid> CreateCategoryAsync(HttpClient client, Guid? parentId = null) =>
        client.PostJsonAsync(Categories, new { code = DatabaseFixture.Code("CAT"), nameAr = "تصنيف", nameEn = (string?)null, parentId })
            .CreatedIdAsync();

    public static Task<Guid> CreateProductAsync(HttpClient client, Guid baseUnitId, Guid? categoryId = null) =>
        client.PostJsonAsync(Products, new
        {
            sku = DatabaseFixture.Code("SKU"), nameAr = "صنف", nameEn = "Item", baseUnitId, categoryId,
            type = ProductType.StockItem, trackingMode = TrackingMode.None,
        }).CreatedIdAsync();

    public static Task<Guid> AddUnitAsync(HttpClient client, Guid productId, Guid unitId, decimal conversionFactor) =>
        client.PostJsonAsync($"{Products}/{productId}/units", new { unitId, conversionFactor }).CreatedIdAsync();

    public static Task<HttpResponseMessage> AddBarcodeAsync(
        HttpClient client, Guid productId, Guid productUnitId, string barcode, BarcodeType type = BarcodeType.Internal) =>
        client.PostJsonAsync($"{Products}/{productId}/units/{productUnitId}/barcodes", new { barcode, barcodeType = type });

    public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {url}: expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task AssertNoContentAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        Assert.True(response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 204, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    public static Task<HttpResponseMessage> PostEmptyAsync(this HttpClient client, string url) => client.PostAsync(url, null);

    /// <summary>A fresh, valid EAN-13: twelve random digits and their GS1 check digit.</summary>
    public static string NewEan13()
    {
        var body = string.Concat(Enumerable.Range(0, 12).Select(_ => (char)('0' + Random.Shared.Next(10))));
        var sum = body.Reverse().Select((c, i) => (c - '0') * (i % 2 == 0 ? 3 : 1)).Sum();
        return body + (char)('0' + (10 - sum % 10) % 10);
    }
}
