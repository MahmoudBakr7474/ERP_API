using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the Inventory API tests. Each test builds its own branch, warehouse and products, since the tests
/// share one database. Approvers may approve their own documents; a second user shows that anyone with the permission can.
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

    public static async Task AssertValuedAsync(HttpClient client, Guid productId, decimal quantity, decimal value)
    {
        var valuation = await ValuationAsync(client, productId);
        Assert.Equal((quantity, value), (valuation.GetProperty("totalQuantity").GetDecimal(), valuation.GetProperty("totalValue").GetDecimal()));
    }

    /// <summary>The product's stock in a warehouse, or in one of its locations.</summary>
    public static async Task<decimal> QuantityAsync(HttpClient client, Guid warehouseId, Guid productId, Guid? locationId = null)
    {
        var url = $"/api/inventory/balances?warehouseId={warehouseId}&productId={productId}"
                  + (locationId is { } l ? $"&locationId={l}" : "");
        return (await client.GetJsonAsync(url)).GetProperty("items").EnumerateArray().Sum(b => b.GetProperty("quantity").GetDecimal());
    }

    public static async Task<Guid> DefaultLocationAsync(HttpClient client, Guid warehouseId) =>
        (await client.GetJsonAsync($"{Warehouses}/{warehouseId}/locations"))
            .GetProperty("items").EnumerateArray().Single(l => l.GetProperty("isDefault").GetBoolean()).GetProperty("id").GetGuid();

    public static Task<Guid> AddLocationAsync(HttpClient admin, Guid warehouseId) =>
        admin.PostJsonAsync($"{Warehouses}/{warehouseId}/locations", new { code = DatabaseFixture.Code("L")[..12], nameAr = "رف", nameEn = (string?)null })
            .CreatedIdAsync();

    // ---------------------------------------------------------------- transfers

    public const string Transfers = "/api/inventory/transfers";

    /// <summary>A stockroom, another physical warehouse of its branch (with its DEFAULT location) and the branch's transit warehouse.</summary>
    public sealed record TransferRoute(Stockroom Source, Guid DestinationId, Guid DestinationLocationId, Guid TransitId);

    public static async Task<TransferRoute> CreateRouteAsync(DatabaseFixture fx, HttpClient admin)
    {
        var source = await CreateStockroomAsync(fx, admin);
        var destination = await CreateWarehouseAsync(admin, source.BranchId);
        var transit = await CreateWarehouseAsync(admin, source.BranchId, "Transit");
        return new TransferRoute(source, destination, await DefaultLocationAsync(admin, destination), transit);
    }

    public static object TransferLine(Guid productId, Guid productUnitId, decimal quantity, Guid? sourceLocationId = null) =>
        new { productId, productUnitId, quantity, sourceLocationId };

    /// <summary>Drafts a transfer along the route; the transit warehouse is left to the branch's default.</summary>
    public static Task<HttpResponseMessage> DraftTransferAsync(HttpClient client, TransferRoute route, params object[] lines) =>
        client.PostJsonAsync(Transfers, new
        {
            sourceWarehouseId = route.Source.WarehouseId, destinationWarehouseId = route.DestinationId, transitWarehouseId = (Guid?)null,
            externalReference = "WAYBILL-1", notes = (string?)null, lines,
        });

    public static Task<JsonElement> TransferAsync(HttpClient client, Guid id) => client.GetJsonAsync($"{Transfers}/{id}");

    /// <summary>An action on a transfer with the row version the client last read; dispatch and approve carry a key.</summary>
    public static async Task<HttpResponseMessage> TransferActAsync(
        HttpClient client, Guid id, string action, Guid? idempotencyKey = null, object? body = null, string? rowVersion = null)
    {
        rowVersion ??= (await TransferAsync(client, id)).GetProperty("rowVersion").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Transfers}/{id}/{action}")
        {
            Content = JsonContent.Create(body ?? new { rowVersion }, options: HttpTesting.Json),
        };
        if (action is "dispatch" or "approve" or "receive")
            request.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }

    /// <summary>Receives base quantities of transfer lines into the destination's DEFAULT location.</summary>
    public static async Task<HttpResponseMessage> ReceiveTransferAsync(
        HttpClient client, Guid id, Guid? idempotencyKey, string? rowVersion, params (Guid LineId, decimal Quantity)[] lines)
    {
        rowVersion ??= (await TransferAsync(client, id)).GetProperty("rowVersion").GetString();
        var body = new { lines = lines.Select(l => new { lineId = l.LineId, quantity = l.Quantity, locationId = (Guid?)null }), rowVersion };
        return await TransferActAsync(client, id, "receive", idempotencyKey, body, rowVersion);
    }

    public static async Task<HttpResponseMessage> CloseTransferAsync(HttpClient client, Guid id, AdjustmentReason reason = AdjustmentReason.Loss)
    {
        var rowVersion = (await TransferAsync(client, id)).GetProperty("rowVersion").GetString();
        return await TransferActAsync(client, id, "close", body: new { reason, notes = "never arrived", rowVersion }, rowVersion: rowVersion);
    }

    /// <summary>Drafts a transfer of pieces along the route and dispatches it; returns its id and its one line's id.</summary>
    public static async Task<(Guid Id, Guid LineId)> DispatchPiecesAsync(HttpClient client, TransferRoute route, decimal pieces)
    {
        var id = await DraftTransferAsync(client, route, TransferLine(route.Source.ProductId, route.Source.Piece, pieces)).CreatedIdAsync();
        await TransferActAsync(client, id, "dispatch").PostedAsync();
        return (id, (await TransferAsync(client, id)).GetProperty("lines")[0].GetProperty("id").GetGuid());
    }

    // ---------------------------------------------------------------- counts

    public const string Counts = "/api/inventory/counts";

    /// <summary>Drafts a count of these locations, or of the whole warehouse when none are given.</summary>
    public static Task<HttpResponseMessage> DraftCountAsync(HttpClient client, Guid warehouseId, params Guid[] locationIds) =>
        client.PostJsonAsync(Counts, new { warehouseId, locationIds, notes = (string?)null });

    public static Task<JsonElement> CountAsync(HttpClient client, Guid id) => client.GetJsonAsync($"{Counts}/{id}");

    /// <summary>An action on a count with the row version the client last read; "lines" is a PUT, approve carries a key.</summary>
    public static async Task<HttpResponseMessage> CountActAsync(
        HttpClient client, Guid id, string action, Guid? idempotencyKey = null, object? body = null, string? rowVersion = null)
    {
        rowVersion ??= (await CountAsync(client, id)).GetProperty("rowVersion").GetString();
        using var request = new HttpRequestMessage(action == "lines" ? HttpMethod.Put : HttpMethod.Post, $"{Counts}/{id}/{action}")
        {
            Content = JsonContent.Create(body ?? new { rowVersion }, options: HttpTesting.Json),
        };
        if (action is "approve")
            request.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }

    public static object Counted(Guid productId, Guid locationId, decimal countedQuantity, decimal? unitCost = null) =>
        new { productId, locationId, countedQuantity, unitCost };

    public static async Task<HttpResponseMessage> EnterCountsAsync(HttpClient client, Guid id, params object[] lines)
    {
        var rowVersion = (await CountAsync(client, id)).GetProperty("rowVersion").GetString();
        return await CountActAsync(client, id, "lines", body: new { lines, rowVersion }, rowVersion: rowVersion);
    }

    public static async Task<HttpResponseMessage> EnsureOkAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return response;
    }
}
