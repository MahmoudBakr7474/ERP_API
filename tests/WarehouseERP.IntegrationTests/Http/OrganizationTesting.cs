using System.Net;
using System.Text.Json;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>Seeds branches, warehouses and locations through the API for the Organization report and import tests.</summary>
internal static class OrganizationTesting
{
    public const string Branches = "/api/organization/branches";
    public const string Warehouses = "/api/organization/warehouses";
    public const string LocationDirectory = "/api/organization/locations";
    public const string OrgReports = "/api/organization/reports";
    public const string OrgExports = "/api/organization/exports";
    public const string LocationImports = "/api/organization/imports/locations";
    public const string Company = "/api/organization/company";
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static readonly string AllPermissions = string.Join(',', OrganizationPermissions.All);

    /// <summary>A short unique code; branch and warehouse codes are at most 30 characters.</summary>
    public static string Code(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    public static async Task<(Guid Id, string Code)> CreateBranchAsync(HttpClient admin, string? nameEn = "Branch")
    {
        var code = Code("B");
        var id = await admin.PostJsonAsync(Branches, new { code, nameAr = $"فرع {code}", nameEn }).CreatedIdAsync();
        return (id, code);
    }

    public static async Task<(Guid Id, string Code)> CreateWarehouseAsync(
        HttpClient admin, Guid branchId, WarehouseType type = WarehouseType.Physical, bool allowsSelling = false, string? nameEn = "Store",
        string? code = null)
    {
        code ??= Code("W");
        var id = await admin.PostJsonAsync(Warehouses, new { branchId, code, nameAr = $"مخزن {code}", nameEn, type, allowsSelling })
            .CreatedIdAsync();
        return (id, code);
    }

    public static async Task<Guid> CreateLocationAsync(HttpClient admin, Guid warehouseId, string code, string nameAr = "رف", string? nameEn = "Shelf") =>
        await admin.PostJsonAsync($"{Warehouses}/{warehouseId}/locations", new { code, nameAr, nameEn }).CreatedIdAsync();

    public static IEnumerable<JsonElement> Items(this JsonElement page) => page.GetProperty("items").EnumerateArray();
}
