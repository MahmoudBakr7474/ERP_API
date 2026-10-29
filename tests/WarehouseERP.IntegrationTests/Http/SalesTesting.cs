using System.Net.Http.Json;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the Sales API tests. Each test builds its own stockroom (branch, selling warehouse, product), customer
/// and prices, since the tests share one database.
/// </summary>
internal static class SalesTesting
{
    public const string Customers = "/api/sales/customers";
    public const string Prices = "/api/sales/prices";
    public const string Settings = "/api/sales/settings";
    public const string Quotations = "/api/sales/quotations";
    public const string Orders = "/api/sales/orders";
    public const string Deliveries = "/api/sales/deliveries";
    public const string Invoices = "/api/sales/invoices";
    public const string Returns = "/api/sales/returns";

    public static readonly string AllPermissions = string.Join(',',
        OrganizationPermissions.All.Concat(CatalogPermissions.All).Concat(InventoryPermissions.All).Concat(SalesPermissions.All));

    /// <summary>The fixture's company date: invoices may not be dated after it.</summary>
    public static readonly DateOnly Today = new(2026, 1, 15);

    public static Task<Guid> CreateCustomerAsync(HttpClient client, string nameAr = "عميل الاختبار", string? taxNumber = null) =>
        client.PostJsonAsync(Customers, new
        {
            code = DatabaseFixture.Code("C"), customerType = CustomerType.Business, nameAr, nameEn = (string?)null, countryCode = "EG",
            taxRegistrationNumber = taxNumber, commercialRegistrationNumber = (string?)null, commercialRegistry = (string?)null,
            phone = (string?)null, mobile = (string?)null, email = (string?)null, notes = (string?)null,
        }).CreatedIdAsync();

    public static async Task SetPriceAsync(HttpClient client, Guid productUnitId, decimal unitPrice)
    {
        var response = await client.PutJsonAsync($"{Prices}/{productUnitId}", new { unitPrice });
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NoContent,
            $"Expected 204, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    public static object Line(Guid productId, Guid productUnitId, decimal quantity, decimal? unitPrice = null, decimal discountPercent = 0,
        SalesTaxTreatment? taxTreatment = null, decimal? taxRate = null) =>
        new { productId, productUnitId, quantity, unitPrice, discountPercent, taxTreatment, taxRate };

    public static Task<HttpResponseMessage> DraftOrderAsync(HttpClient client, Guid customerId, Guid warehouseId, params object[] lines) =>
        client.PostJsonAsync(Orders, new
        {
            customerId, warehouseId, orderDate = (DateOnly?)null, requestedDeliveryDate = (DateOnly?)null, customerReference = "PO-77",
            paymentTermDays = (int?)null, shippingAddressId = (Guid?)null, notes = (string?)null, lines,
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

    /// <summary>Drafts an order and submits it, which confirms and reserves it under the default settings.</summary>
    public static async Task<(Guid OrderId, Guid[] LineIds)> ConfirmedOrderAsync(HttpClient client, Guid customerId, Guid warehouseId, params object[] lines)
    {
        var id = await DraftOrderAsync(client, customerId, warehouseId, lines).CreatedIdAsync();
        await ActAsync(client, Orders, id, "submit").EnsureOkAsync();
        var order = await GetAsync(client, Orders, id);
        Assert.Equal("Confirmed", order.GetProperty("status").GetString());
        return (id, order.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("id").GetGuid()).ToArray());
    }

    public static Task<HttpResponseMessage> DraftDeliveryAsync(HttpClient client, Guid orderId, params (Guid OrderLineId, decimal Quantity, Guid? LocationId)[] lines) =>
        client.PostJsonAsync(Deliveries, new
        {
            orderId, notes = (string?)null,
            lines = lines.Select(l => new { orderLineId = l.OrderLineId, quantity = l.Quantity, locationId = l.LocationId }),
        });

    /// <summary>Drafts a delivery and posts it with a new key; returns the delivery id.</summary>
    public static async Task<Guid> DeliverAsync(HttpClient client, Guid orderId, params (Guid OrderLineId, decimal Quantity, Guid? LocationId)[] lines)
    {
        var id = await DraftDeliveryAsync(client, orderId, lines).CreatedIdAsync();
        await ActAsync(client, Deliveries, id, "post", idempotencyKey: Guid.NewGuid()).EnsureOkAsync();
        return id;
    }

    public static Task<HttpResponseMessage> DraftInvoiceAsync(HttpClient client, Guid orderId, params (Guid OrderLineId, decimal Quantity)[] lines) =>
        client.PostJsonAsync(Invoices, new
        {
            orderId, invoiceDate = Today, customerReference = (string?)null, notes = (string?)null,
            lines = lines.Select(l => new { orderLineId = l.OrderLineId, quantity = l.Quantity }),
        });

    public static Task<HttpResponseMessage> DraftReturnAsync(
        HttpClient client, Guid deliveryId, params (Guid DeliveryLineId, decimal Quantity, ReturnCondition Condition)[] lines) =>
        client.PostJsonAsync(Returns, new
        {
            deliveryId, reason = "الصنف غير مطابق", notes = (string?)null,
            lines = lines.Select(l => new { deliveryLineId = l.DeliveryLineId, quantity = l.Quantity, condition = l.Condition, locationId = (Guid?)null }),
        });

    /// <summary>The warehouse's on-hand sellable and reserved quantity of the product, from Inventory's availability.</summary>
    public static async Task<(decimal OnHand, decimal Reserved)> AvailabilityAsync(HttpClient client, Guid warehouseId, Guid productId)
    {
        var row = (await client.GetJsonAsync($"/api/inventory/availability?warehouseId={warehouseId}&productId={productId}&includeZero=true"))
            .GetProperty("items").EnumerateArray().Single();
        return (row.GetProperty("onHandSellable").GetDecimal(), row.GetProperty("reservedQuantity").GetDecimal());
    }

    public static decimal Decimal(this JsonElement element, string property) => element.GetProperty(property).GetDecimal();

    public static string? Text(this JsonElement element, string property) => element.GetProperty(property).GetString();
}
