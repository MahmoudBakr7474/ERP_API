using System.Net.Http.Json;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the Purchasing API tests. Each test builds its own stockroom (branch, warehouse, product) and
/// supplier, since the tests share one database.
/// </summary>
internal static class PurchasingTesting
{
    public const string Suppliers = "/api/purchasing/suppliers";
    public const string Orders = "/api/purchasing/orders";
    public const string Receipts = "/api/purchasing/receipts";
    public const string Invoices = "/api/purchasing/invoices";
    public const string Returns = "/api/purchasing/returns";
    public const string Requests = "/api/purchasing/requests";
    public const string Policies = "/api/purchasing/policies";

    public static readonly string AllPermissions = string.Join(',',
        OrganizationPermissions.All.Concat(CatalogPermissions.All).Concat(InventoryPermissions.All).Concat(PurchasingPermissions.All));

    /// <summary>The fixture's company date: invoices may not be dated after it.</summary>
    public static readonly DateOnly Today = new(2026, 1, 15);

    public static Task<Guid> CreateSupplierAsync(HttpClient client, string? taxNumber = null, string nameAr = "مورد الاختبار") =>
        client.PostJsonAsync(Suppliers, new
        {
            code = DatabaseFixture.Code("S"), nameAr, nameEn = (string?)null, countryCode = "EG", taxRegistrationNumber = taxNumber,
            commercialRegistrationNumber = (string?)null, commercialRegistry = (string?)null, paymentTermDays = 30, phone = (string?)null,
            email = (string?)null, notes = (string?)null,
        }).CreatedIdAsync();

    public static object OrderLine(Guid productId, Guid productUnitId, decimal quantity, decimal unitPrice, decimal discountPercent = 0,
        decimal taxRate = 0, Guid? purchaseRequestLineId = null, string taxTreatment = "Recoverable") =>
        new { productId, productUnitId, quantity, unitPrice, discountPercent, taxRate, taxTreatment, purchaseRequestLineId };

    public static Task<HttpResponseMessage> DraftOrderAsync(HttpClient client, Guid supplierId, Guid warehouseId, params object[] lines) =>
        client.PostJsonAsync(Orders, new
        {
            supplierId, warehouseId, expectedDeliveryDate = (DateOnly?)null, supplierReference = "Q-1", paymentTermDays = (int?)null,
            notes = (string?)null, lines,
        });

    public static Task<JsonElement> GetAsync(HttpClient client, string route, Guid id) => client.GetJsonAsync($"{route}/{id}");

    /// <summary>An action with the row version the client last read; posting actions carry an Idempotency-Key.</summary>
    public static async Task<HttpResponseMessage> ActAsync(
        HttpClient client, string route, Guid id, string action, object? body = null, Guid? idempotencyKey = null, string? rowVersion = null)
    {
        rowVersion ??= (await GetAsync(client, route, id)).GetProperty("rowVersion").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{route}/{id}/{action}")
        {
            Content = JsonContent.Create(body ?? new { rowVersion }, options: HttpTesting.Json),
        };
        if (idempotencyKey is { } key)
            request.Headers.Add("Idempotency-Key", key.ToString());
        return await client.SendAsync(request);
    }

    /// <summary>Drafts an order, submits it and has the same user approve it; returns the order and its line ids.</summary>
    public static async Task<(Guid OrderId, Guid[] LineIds)> ApprovedOrderAsync(HttpClient client, Guid supplierId, Guid warehouseId, params object[] lines)
    {
        var id = await DraftOrderAsync(client, supplierId, warehouseId, lines).CreatedIdAsync();
        await ActAsync(client, Orders, id, "submit").EnsureOkAsync();
        await ActAsync(client, Orders, id, "approve").EnsureOkAsync();
        var order = await GetAsync(client, Orders, id);
        return (id, order.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("id").GetGuid()).ToArray());
    }

    public static async Task<HttpResponseMessage> ReceiveAsync(
        HttpClient client, Guid orderId, Guid idempotencyKey, params (Guid LineId, decimal Quantity)[] lines)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Orders}/{orderId}/receipts")
        {
            Content = JsonContent.Create(new
            {
                supplierDeliveryNote = "DN-1", notes = (string?)null,
                lines = lines.Select(l => new { purchaseOrderLineId = l.LineId, quantity = l.Quantity, locationId = (Guid?)null }),
            }, options: HttpTesting.Json),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        return await client.SendAsync(request);
    }

    public static object InvoiceLine(Guid orderLineId, decimal quantity, decimal unitPrice, decimal taxRate = 0) =>
        new { purchaseOrderLineId = orderLineId, quantity, unitPrice, discountPercent = 0m, taxRate, taxTreatment = "Recoverable" };

    public static Task<HttpResponseMessage> DraftInvoiceAsync(HttpClient client, Guid orderId, string number, params object[] lines) =>
        client.PostJsonAsync(Invoices, new
        {
            purchaseOrderId = orderId, supplierInvoiceNumber = number, invoiceDate = Today, dueDate = (DateOnly?)null, notes = (string?)null, lines,
        });

    /// <summary>The row version an update answered with, after asserting it succeeded.</summary>
    public static async Task<string> ReadUpdatedAsync(this Task<HttpResponseMessage> pending)
    {
        var response = await pending.EnsureOkAsync();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(HttpTesting.Json)).GetProperty("rowVersion").GetString()!;
    }

    public static Task<HttpResponseMessage> DraftReturnAsync(HttpClient client, Guid receiptId, params (Guid LineId, decimal Quantity)[] lines) =>
        client.PostJsonAsync(Returns, new
        {
            goodsReceiptId = receiptId, reason = "damaged in transport", notes = (string?)null,
            lines = lines.Select(l => new { goodsReceiptLineId = l.LineId, quantity = l.Quantity, locationId = (Guid?)null }),
        });
}
