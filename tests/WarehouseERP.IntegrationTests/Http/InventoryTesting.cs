using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the Inventory API tests. Each test builds its own branch, warehouse and products, since the tests
/// share one database; two users are needed because nobody approves their own document.
/// </summary>
internal static class InventoryTesting
{
    public const string Documents = "/api/inventory/documents";
    public const string Warehouses = "/api/organization/warehouses";

    public static readonly Guid Approver = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly string AllPermissions = string.Join(',',
        OrganizationPermissions.All.Concat(CatalogPermissions.All).Concat(InventoryPermissions.All));

    /// <summary>A warehouse with its DEFAULT location and a product counted in pieces, with a carton of 12.</summary>
    public sealed record Stockroom(Guid BranchId, Guid WarehouseId, Guid DefaultLocationId, Guid ProductId, Guid Piece, Guid Carton);

    public static async Task<Stockroom> CreateStockroomAsync(DatabaseFixture fx, HttpClient admin)
    {
        var (branchId, pieceUnitId) = await fx.SeedBranchAndUnitAsync();
        var warehouseId = await CreateWarehouseAsync(admin, branchId);
        var defaultLocation = (await admin.GetJsonAsync($"{Warehouses}/{warehouseId}/locations"))
            .GetProperty("items").EnumerateArray().Single(l => l.GetProperty("isDefault").GetBoolean()).GetProperty("id").GetGuid();
        var (productId, piece) = await CreateProductAsync(admin, pieceUnitId);
        var carton = await CatalogTesting.AddUnitAsync(admin, productId, await CatalogTesting.CreateUnitAsync(admin), 12m);
        return new Stockroom(branchId, warehouseId, defaultLocation, productId, piece, carton);
    }

    public static Task<Guid> CreateWarehouseAsync(HttpClient admin, Guid branchId, string type = "Physical") =>
        admin.PostJsonAsync(Warehouses, new
        {
            branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type, allowsSelling = type == "Physical",
        }).CreatedIdAsync();

    /// <summary>A stock item counted in the given unit; returns the product and its base product unit.</summary>
    public static async Task<(Guid ProductId, Guid BaseUnit)> CreateProductAsync(
        HttpClient admin, Guid baseUnitId, ProductType type = ProductType.StockItem, TrackingMode tracking = TrackingMode.None)
    {
        var productId = await admin.PostJsonAsync(CatalogTesting.Products, new
        {
            sku = DatabaseFixture.Code("SKU"), nameAr = "صنف", nameEn = "Item", baseUnitId, categoryId = (Guid?)null, type,
            trackingMode = tracking,
        }).CreatedIdAsync();
        var baseUnit = (await admin.GetJsonAsync($"{CatalogTesting.Products}/{productId}"))
            .GetProperty("units").EnumerateArray().Single(u => u.GetProperty("isBase").GetBoolean()).GetProperty("id").GetGuid();
        return (productId, baseUnit);
    }

    public static object Line(Guid productId, Guid productUnitId, decimal quantity, decimal? unitCost = null,
        StockDirection? direction = null, Guid? locationId = null) =>
        new { productId, productUnitId, quantity, locationId, unitCost, direction };

    public static Task<HttpResponseMessage> DraftAsync(
        HttpClient client, StockDocumentType type, Guid warehouseId, AdjustmentReason? reason, params object[] lines) =>
        client.PostJsonAsync(Documents, new
        {
            documentType = type, warehouseId, adjustmentReason = reason, externalReference = "EXT-1", notes = (string?)null, lines,
        });

    public static async Task<Guid> DraftIdAsync(
        HttpClient client, StockDocumentType type, Guid warehouseId, AdjustmentReason? reason, params object[] lines) =>
        await DraftAsync(client, type, warehouseId, reason, lines).CreatedIdAsync();

    public static Task<JsonElement> DocumentAsync(HttpClient client, Guid id) => client.GetJsonAsync($"{Documents}/{id}");

    /// <summary>An action on a document with the row version the client last read; post and approve carry a key.</summary>
    public static async Task<HttpResponseMessage> ActAsync(
        HttpClient client, Guid id, string action, Guid? idempotencyKey = null, object? body = null, string? rowVersion = null)
    {
        rowVersion ??= (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Documents}/{id}/{action}")
        {
            Content = JsonContent.Create(body ?? new { rowVersion }, options: HttpTesting.Json),
        };
        if (action is "post" or "approve")
            request.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }

    public static async Task<JsonElement> PostedAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Drafts and posts a receipt directly; returns the document id.</summary>
    public static async Task<Guid> ReceiveAsync(HttpClient client, Stockroom room, decimal pieces, decimal unitCost, Guid? locationId = null)
    {
        var id = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null,
            Line(room.ProductId, room.Piece, pieces, unitCost, locationId: locationId));
        await ActAsync(client, id, "post").PostedAsync();
        return id;
    }

    /// <summary>Drafts and posts an issue directly; returns the response.</summary>
    public static async Task<HttpResponseMessage> IssueAsync(HttpClient client, Stockroom room, decimal pieces, Guid? locationId = null)
    {
        var id = await DraftIdAsync(client, StockDocumentType.Issue, room.WarehouseId, null,
            Line(room.ProductId, room.Piece, pieces, locationId: locationId));
        return await ActAsync(client, id, "post");
    }

    public static async Task<decimal> BalanceAsync(HttpClient client, Stockroom room, Guid? locationId = null)
    {
        var url = $"/api/inventory/balances?warehouseId={room.WarehouseId}&productId={room.ProductId}"
                  + (locationId is { } l ? $"&locationId={l}" : "");
        return (await client.GetJsonAsync(url)).GetProperty("items").EnumerateArray().Sum(b => b.GetProperty("quantity").GetDecimal());
    }

    public static async Task<JsonElement> ValuationAsync(HttpClient client, Guid productId) =>
        (await client.GetJsonAsync($"/api/inventory/valuation?productId={productId}&includeZero=true"))
            .GetProperty("items").EnumerateArray().Single();
}
