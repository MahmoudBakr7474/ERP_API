using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Sales.Contracts;
using Stockroom = WarehouseERP.IntegrationTests.Http.InventoryTesting.Stockroom;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the POS API tests. Each test builds its own till: a stockroom with stock at cost 6, a list price of 10
/// per piece, the product classified standard-rated (14%), a terminal selling from the warehouse's DEFAULT location and,
/// when asked, a cashier's shift opened with a float of 100.
/// </summary>
internal static class PosTesting
{
    public const string Terminals = "/api/pos/terminals";
    public const string Shifts = "/api/pos/shifts";
    public const string SalesRoute = "/api/pos/sales";
    public const string Returns = "/api/pos/returns";
    public const string Settings = "/api/pos/settings";
    public const string CardIncidents = "/api/pos/card-incidents";
    public const string ProductTaxes = "/api/sales/product-taxes";

    public static readonly string AllPermissions = string.Join(',',
        OrganizationPermissions.All.Concat(CatalogPermissions.All).Concat(InventoryPermissions.All).Concat(SalesPermissions.All)
            .Concat(PosPermissions.All));

    /// <summary>What a cashier holds: selling, reading, discounts, cash and refunds, nothing else.</summary>
    public static readonly string CashierPermissions = string.Join(',',
        PosPermissions.Sell, PosPermissions.Read, PosPermissions.DiscountsApply, PosPermissions.CashManage, PosPermissions.ReturnsManage);

    public sealed record Till(Stockroom Room, Guid TerminalId, string TerminalCode);

    public static async Task<Till> CreateTillAsync(DatabaseFixture fx, HttpClient admin, decimal stock = 10m, bool acceptsCard = true)
    {
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        if (stock > 0)
            await InventoryTesting.ReceiveAsync(admin, room, stock, 6m);
        await SalesTesting.SetPriceAsync(admin, room.Piece, 10m);
        await ClassifyAsync(admin, room.ProductId, SalesTaxTreatment.Standard);
        var code = DatabaseFixture.Code("T")[..12].ToUpperInvariant();
        var terminalId = await admin.PostJsonAsync(Terminals, new
        {
            code, name = "كاشير 1", warehouseId = room.WarehouseId, sellingLocationId = (Guid?)null, acceptsCash = true, acceptsCard,
            receiptHeader = "متجر الاختبار", receiptFooter = "شكراً لزيارتكم",
        }).CreatedIdAsync();
        return new Till(room, terminalId, code);
    }

    public static async Task ClassifyAsync(HttpClient admin, Guid productId, SalesTaxTreatment treatment, decimal? taxRate = null)
    {
        var response = await admin.PutJsonAsync($"{ProductTaxes}/{productId}", new { taxTreatment = treatment, taxRate });
        Assert.True(response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 204, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    public static Task<HttpResponseMessage> OpenShiftAsync(HttpClient client, Guid terminalId, decimal openingFloat = 100m) =>
        client.PostJsonAsync(Shifts, new { terminalId, openingFloat });

    public static object CartLine(Guid productId, Guid productUnitId, decimal quantity, decimal discountPercent = 0m) =>
        new { productId, productUnitId, quantity, discountPercent };

    public static object Cash(decimal amount) => new { method = PosPaymentMethod.Cash, amount, reference = (string?)null, cardLast4 = (string?)null };

    public static object Card(decimal amount, string? reference = "AUTH-123456", string? last4 = "4242") =>
        new { method = PosPaymentMethod.Card, amount, reference, cardLast4 = last4 };

    public static object[] Lines(params object[] lines) => lines;

    public static object[] Pay(params object[] payments) => payments;

    /// <summary>A checkout; a new Idempotency-Key unless one is given, none at all when <paramref name="withoutKey"/>.</summary>
    public static async Task<HttpResponseMessage> CheckoutAsync(
        HttpClient client, Guid terminalId, object[] lines, object[] payments, Guid? key = null, Guid? customerId = null,
        string? customerName = null, bool withoutKey = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SalesRoute)
        {
            Content = JsonContent.Create(new
            {
                terminalId, customerId, customerName, customerPhone = (string?)null, notes = (string?)null, lines, payments,
            }, options: HttpTesting.Json),
        };
        if (!withoutKey)
            request.Headers.Add("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }

    public static async Task<JsonElement> SoldAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task<HttpResponseMessage> ReturnAsync(
        HttpClient client, Guid terminalId, Guid saleId, object[] lines, object[] refunds, Guid? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Returns)
        {
            Content = JsonContent.Create(new { terminalId, saleId, reason = "مقاس غير مناسب", notes = (string?)null, lines, refunds },
                options: HttpTesting.Json),
        };
        request.Headers.Add("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }

    public static object ReturnLine(Guid saleLineId, decimal quantity, PosReturnCondition condition = PosReturnCondition.FitForResale) =>
        new { saleLineId, quantity, condition };

    public static Task<JsonElement> ShiftAsync(HttpClient client, Guid shiftId) => client.GetJsonAsync($"{Shifts}/{shiftId}");

    public static Task<JsonElement> CurrentShiftAsync(HttpClient client, Guid terminalId) =>
        client.GetJsonAsync($"{Terminals}/{terminalId}/current-shift");

    public static async Task<HttpResponseMessage> CashMovementAsync(HttpClient client, Guid shiftId, PosCashMovementType type, decimal amount, string reason)
    {
        var rowVersion = (await ShiftAsync(client, shiftId)).Text("rowVersion");
        return await client.PostJsonAsync($"{Shifts}/{shiftId}/cash-movements", new { movementType = type, amount, reason, rowVersion });
    }

    public static async Task<HttpResponseMessage> CloseShiftAsync(HttpClient client, Guid shiftId, decimal countedCash, string? note, HttpClient? reader = null)
    {
        var rowVersion = (await ShiftAsync(reader ?? client, shiftId)).Text("rowVersion");
        return await client.PostJsonAsync($"{Shifts}/{shiftId}/close", new { countedCash, note, rowVersion });
    }
}
