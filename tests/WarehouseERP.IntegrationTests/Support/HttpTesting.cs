using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WarehouseERP.IntegrationTests;

internal static class HttpTesting
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>A client acting as a user with these permissions; warehouses "*" means all.</summary>
    public static HttpClient As(this HttpClient client, string permissions, string warehouses = "*")
    {
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, UserId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, permissions);
        client.DefaultRequestHeaders.Add(TestAuthHandler.WarehousesHeader, warehouses);
        return client;
    }

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    /// <summary>Asserts a ProblemDetails response with this status and errorCode; returns its JSON.</summary>
    public static async Task<JsonElement> AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonDocument.Parse(body).RootElement.Clone();
        Assert.Equal(errorCode, problem.GetProperty("errorCode").GetString());
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _), "ProblemDetails has no traceId.");
        return problem;
    }
}
