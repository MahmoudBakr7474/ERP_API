using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogFiles;
using static WarehouseERP.IntegrationTests.Http.OrganizationTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Organization lists, reports and files from the real host: branch names and counts on the lists, the whole filtered
// result in every file (not the screen's page), warehouse scope everywhere, data-quality issues that only real data
// states produce, and the company profile from the company's own recorded details.
[Collection(DatabaseCollection.Name)]
public sealed class OrganizationReportApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        await fx.EnsureCompanyAsync();
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    [Fact]
    public async Task Lists_carry_branch_names_counts_and_the_new_filters_and_order()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (empty, emptyCode) = await CreateBranchAsync(admin);
        var (selling, _) = await CreateWarehouseAsync(admin, branchId, allowsSelling: true);
        var (store, _) = await CreateWarehouseAsync(admin, branchId);
        await CreateWarehouseAsync(admin, branchId, WarehouseType.Transit);
        await CreateLocationAsync(admin, store, "A-01");
        await admin.PostAsync($"{Warehouses}/{store}/deactivate", null).AssertNoContentAsync();

        var branch = Assert.Single((await admin.GetJsonAsync($"{Branches}?search={branchCode}")).Items());
        Assert.Equal((3, 2), (branch.GetProperty("warehouseCount").GetInt32(), branch.GetProperty("activeWarehouseCount").GetInt32()));
        var without = Assert.Single((await admin.GetJsonAsync($"{Branches}?hasWarehouses=false&search={emptyCode}")).Items());
        Assert.Equal((empty, 0), (without.GetProperty("id").GetGuid(), without.GetProperty("warehouseCount").GetInt32()));
        Assert.Empty((await admin.GetJsonAsync($"{Branches}?hasWarehouses=false&search={branchCode}")).Items());

        var sells = Assert.Single((await admin.GetJsonAsync($"{Warehouses}?branchId={branchId}&type=Physical&allowsSelling=true")).Items());
        Assert.Equal((selling, branchCode, $"فرع {branchCode}"),
            (sells.GetProperty("id").GetGuid(), sells.GetProperty("branchCode").GetString(), sells.GetProperty("branchNameAr").GetString()));
        var inactive = Assert.Single((await admin.GetJsonAsync($"{Warehouses}?branchId={branchId}&isActive=false")).Items());
        Assert.Equal((store, 2, 2, "DEFAULT", true),
            (inactive.GetProperty("id").GetGuid(), inactive.GetProperty("locationCount").GetInt32(), inactive.GetProperty("activeLocationCount").GetInt32(),
                inactive.GetProperty("defaultLocationCode").GetString(), inactive.GetProperty("defaultLocationIsActive").GetBoolean()));

        var byName = (await admin.GetJsonAsync($"{Warehouses}?branchId={branchId}&sort=NameAr&descending=true")).Items()
            .Select(w => w.GetProperty("nameAr").GetString()).ToList();
        Assert.Equal(byName.OrderByDescending(n => n, StringComparer.Ordinal), byName);
        await (await admin.GetAsync($"{Warehouses}?type=9")).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task The_location_directory_and_its_files_hold_the_whole_filtered_result()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (warehouseId, warehouseCode) = await CreateWarehouseAsync(admin, branchId);
        for (var i = 0; i < 24; i++)
            await CreateLocationAsync(admin, warehouseId, $"R-{i:00}", nameEn: i == 0 ? "=HYPERLINK(\"http://evil\")" : "Rack");

        var page = await admin.GetJsonAsync($"{LocationDirectory}?warehouseId={warehouseId}");
        Assert.Equal((25, 20), (page.GetProperty("totalCount").GetInt32(), page.GetProperty("items").GetArrayLength()));
        var first = page.Items().First();
        Assert.Equal(("DEFAULT", true, warehouseCode, branchCode), (first.GetProperty("code").GetString(), first.GetProperty("isDefault").GetBoolean(),
            first.GetProperty("warehouseCode").GetString(), first.GetProperty("branchCode").GetString()));

        using (var workbook = Read(await admin.DownloadAsync($"{OrgExports}/locations?format=Xlsx&lang=En&warehouseId={warehouseId}", Xlsx)))
        {
            var sheet = workbook.Worksheets.First();
            Assert.Equal(26, sheet.LastRowUsed()!.RowNumber()); // header + default + 24
            Assert.Equal("Branch code", sheet.Cell(1, 1).GetString());
            Assert.Equal((branchCode, XLDataType.Text), (sheet.Cell(2, 1).GetString(), sheet.Cell(2, 1).DataType));
            Assert.Equal("DEFAULT", sheet.Cell(2, 6).GetString());
            Assert.True(sheet.SheetView.SplitRow == 1 && sheet.AutoFilter.IsEnabled);
            Assert.Equal("Report info", workbook.Worksheets.Last().Name);
        }

        var csv = await admin.DownloadAsync($"{OrgExports}/locations?format=Csv&lang=Ar&warehouseId={warehouseId}&search=R-0", "text/csv");
        Assert.Equal(Encoding.UTF8.GetPreamble(), csv[..3]);
        var text = Encoding.UTF8.GetString(csv[3..]);
        Assert.StartsWith("كود الفرع,الفرع,كود المخزن", text);
        Assert.Equal(11, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length); // header + R-00..R-09
        Assert.Contains("\"'=HYPERLINK(\"\"http://evil\"\")\"", text);

        var pdf = await admin.DownloadAsync($"{OrgExports}/locations?format=Pdf&lang=Ar&warehouseId={warehouseId}", "application/pdf");
        AssertPdfHas(pdf, "دليل مواقع التخزين");
        Assert.True(Regex.Count(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page\b") >= 1);
    }

    [Fact]
    public async Task Branch_and_warehouse_directories_export_with_their_counts()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (warehouseId, warehouseCode) = await CreateWarehouseAsync(admin, branchId, allowsSelling: true);
        await CreateLocationAsync(admin, warehouseId, "B-1");

        using (var workbook = Read(await admin.DownloadAsync($"{OrgExports}/branches?format=Xlsx&lang=En&search={branchCode}", Xlsx)))
        {
            var sheet = workbook.Worksheets.First();
            Assert.Equal((branchCode, 1d, 1d), (sheet.Cell(2, 1).GetString(), sheet.Cell(2, 5).GetDouble(), sheet.Cell(2, 6).GetDouble()));
            Assert.Equal("Total", sheet.Cell(3, 1).GetString());
        }
        using (var workbook = Read(await admin.DownloadAsync($"{OrgExports}/warehouses?format=Xlsx&lang=En&branchId={branchId}", Xlsx)))
        {
            var sheet = workbook.Worksheets.First();
            Assert.Equal(2, sheet.LastRowUsed()!.RowNumber());
            Assert.Equal((branchCode, warehouseCode, "Yes", "DEFAULT", 2d),
                (sheet.Cell(2, 1).GetString(), sheet.Cell(2, 3).GetString(), sheet.Cell(2, 7).GetString(), sheet.Cell(2, 10).GetString(),
                    sheet.Cell(2, 12).GetDouble()));
            var info = workbook.Worksheets.Last();
            Assert.Contains(info.CellsUsed(), c => c.GetString() == $"{branchCode} · Branch");
        }
        AssertPdfHas(await admin.DownloadAsync($"{OrgExports}/branches?format=Pdf&lang=En&search={branchCode}", "application/pdf"), "Branch directory");
        AssertPdfHas(await admin.DownloadAsync($"{OrgExports}/warehouses?format=Pdf&lang=Ar&branchId={branchId}", "application/pdf"), "دليل المخازن");
    }

    [Fact]
    public async Task A_scoped_user_sees_only_their_warehouses_everywhere()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (mine, _) = await CreateWarehouseAsync(admin, branchId);
        var (other, _) = await CreateWarehouseAsync(admin, branchId);
        await CreateLocationAsync(admin, other, "HIDDEN-1");
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: mine.ToString());

        var branch = Assert.Single((await scoped.GetJsonAsync($"{Branches}?search={branchCode}")).Items());
        Assert.Equal(1, branch.GetProperty("warehouseCount").GetInt32());
        var structure = await scoped.GetJsonAsync($"{OrgReports}/structure");
        var listed = Assert.Single(structure.EnumerateArray());
        Assert.Equal(mine, Assert.Single(listed.GetProperty("warehouses").EnumerateArray()).GetProperty("id").GetGuid());
        var summary = await scoped.GetJsonAsync($"{OrgReports}/summary");
        Assert.Equal((1, 1, 1), (summary.GetProperty("branches").GetInt32(), summary.GetProperty("warehouses").GetInt32(),
            summary.GetProperty("storageLocations").GetInt32()));

        Assert.All((await scoped.GetJsonAsync($"{LocationDirectory}?pageSize=100")).Items(), l => Assert.Equal(mine, l.GetProperty("warehouseId").GetGuid()));
        await (await scoped.GetAsync($"{LocationDirectory}?warehouseId={other}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        await (await scoped.GetAsync($"{OrgExports}/locations?warehouseId={other}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        var csv = Encoding.UTF8.GetString(await scoped.DownloadAsync($"{OrgExports}/locations?format=Csv&lang=En", "text/csv"));
        Assert.DoesNotContain("HIDDEN-1", csv);
        var issues = await scoped.GetJsonAsync($"{OrgReports}/data-quality?pageSize=100");
        Assert.All(issues.GetProperty("issues").Items(), i =>
            Assert.True(i.GetProperty("warehouseId").ValueKind == JsonValueKind.Null || i.GetProperty("warehouseId").GetGuid() == mine));
    }

    [Fact]
    public async Task The_structure_lists_branches_warehouses_and_on_request_every_location()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (warehouseId, warehouseCode) = await CreateWarehouseAsync(admin, branchId);
        var retired = await CreateLocationAsync(admin, warehouseId, "OLD-1");
        await CreateLocationAsync(admin, warehouseId, "NEW-1");
        await admin.PostAsync($"{Warehouses}/{warehouseId}/locations/{retired}/deactivate", null).AssertNoContentAsync();
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: warehouseId.ToString());

        var tree = Assert.Single((await scoped.GetJsonAsync($"{OrgReports}/structure?isActive=true")).EnumerateArray());
        var warehouse = Assert.Single(tree.GetProperty("warehouses").EnumerateArray());
        Assert.Equal((branchCode, warehouseCode, 2, 2, "DEFAULT"),
            (tree.GetProperty("code").GetString(), warehouse.GetProperty("code").GetString(), warehouse.GetProperty("locationCount").GetInt32(),
                warehouse.GetProperty("activeLocationCount").GetInt32(), warehouse.GetProperty("defaultLocationCode").GetString()));

        using (var workbook = Read(await scoped.DownloadAsync($"{OrgExports}/structure?format=Xlsx&lang=En&includeLocations=true", Xlsx)))
        {
            var rows = workbook.Worksheets.First().RowsUsed().Skip(1).Select(r => (r.Cell(1).GetString(), r.Cell(4).GetString())).ToList();
            Assert.Equal([("Branch", branchCode), ("Warehouse", warehouseCode), ("Location", "DEFAULT"), ("Location", "NEW-1"), ("Location", "OLD-1")], rows);
        }
        using (var workbook = Read(await scoped.DownloadAsync($"{OrgExports}/structure?format=Xlsx&lang=En&includeLocations=true&isActive=true", Xlsx)))
            Assert.DoesNotContain(workbook.Worksheets.First().RowsUsed(), r => r.Cell(4).GetString() == "OLD-1");
        AssertPdfHas(await scoped.DownloadAsync($"{OrgExports}/structure?format=Pdf&lang=Ar&includeLocations=true", "application/pdf"), "الهيكل التنظيمي");
    }

    [Fact]
    public async Task Data_quality_reports_only_states_the_data_still_allows()
    {
        var admin = Admin();
        var (lonely, _) = await CreateBranchAsync(admin);
        var (branchId, _) = await CreateBranchAsync(admin);
        var (unnamed, _) = await CreateWarehouseAsync(admin, branchId, nameEn: null);
        var (defaultOff, _) = await CreateWarehouseAsync(admin, branchId);
        await CreateLocationAsync(admin, defaultOff, "NO-EN", nameEn: null);
        var (closedBranch, _) = await CreateBranchAsync(admin);
        var (stranded, _) = await CreateWarehouseAsync(admin, closedBranch);

        // The screens refuse these states; data written before the rules (or by hand) can still hold them.
        await using (var db = fx.NewDb())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE organization.WarehouseLocations SET IsActive = 0 WHERE WarehouseId = {defaultOff} AND IsDefault = 1");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE organization.Branches SET IsActive = 0 WHERE Id = {closedBranch}");
        }

        var report = await admin.GetJsonAsync($"{OrgReports}/data-quality?pageSize=100&issue=InactiveDefaultLocation");
        Assert.Contains(report.GetProperty("issues").Items(), i => i.GetProperty("entityId").GetGuid() == defaultOff);
        var all = new List<JsonElement>();
        for (var page = 1; ; page++)
        {
            var issues = (await admin.GetJsonAsync($"{OrgReports}/data-quality?pageSize=100&page={page}")).GetProperty("issues");
            all.AddRange(issues.Items());
            if (page * 100 >= issues.GetProperty("totalCount").GetInt32())
                break;
        }
        bool Has(string issue, Guid id) => all.Any(i => i.GetProperty("issue").GetString() == issue && i.GetProperty("entityId").GetGuid() == id);
        Assert.True(Has("BranchWithoutActiveWarehouse", lonely));
        Assert.True(Has("MissingEnglishName", unnamed));
        Assert.True(Has("InactiveDefaultLocation", defaultOff));
        Assert.True(Has("ActiveWarehouseInInactiveBranch", stranded));
        Assert.True(Has("NoDefaultTransitWarehouse", branchId));
        Assert.Contains(all, i => i.GetProperty("issue").GetString() == "MissingEnglishName" && i.GetProperty("code").GetString() == "NO-EN"
                                  && i.GetProperty("warehouseId").GetGuid() == defaultOff);
        Assert.False(Has("NoDefaultLocation", unnamed)); // every warehouse gets its DEFAULT location
        Assert.Contains(report.GetProperty("counts").EnumerateArray(), c => c.GetProperty("issue").GetString() == "MissingEnglishName");

        using var workbook = Read(await admin.DownloadAsync($"{OrgExports}/data-quality?format=Xlsx&lang=En&issue=ActiveWarehouseInInactiveBranch", Xlsx));
        Assert.All(workbook.Worksheets.First().RowsUsed().Skip(1), r => Assert.Equal("Active warehouse in an inactive branch", r.Cell(1).GetString()));
    }

    [Fact]
    public async Task The_company_profile_prints_the_recorded_contact_details()
    {
        var admin = _api.CreateClient().As(OrganizationPermissions.CompanyUpdate);
        var cairo = (await admin.GetJsonAsync("/api/organization/governorates?countryCode=EG")).EnumerateArray()
            .Single(g => g.GetProperty("nameEn").GetString() == "Cairo").GetProperty("id").GetGuid();
        var city = (await admin.GetJsonAsync($"/api/organization/cities?governorateId={cairo}")).EnumerateArray().First();
        var before = await admin.GetJsonAsync(Company);
        object Update(string rowVersion, Guid cityId, string email = "info@example.com") => new
        {
            nameAr = before.GetProperty("nameAr").GetString(), nameEn = "Test Trading Co", timeZoneId = "Africa/Cairo",
            taxRegistrationNumber = "100-200-300", rowVersion, commercialRegistrationNumber = "123456", commercialRegistry = "سجل تجاري القاهرة",
            phone = "+20 2 2345 6789", email, addressLine = "5 شارع التحرير، الدور الثالث", cityId,
        };

        await (await admin.PutJsonAsync(Company, Update(before.GetProperty("rowVersion").GetString()!, city.GetProperty("id").GetGuid(), "not-an-email")))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await admin.PutJsonAsync(Company, Update(before.GetProperty("rowVersion").GetString()!, Guid.NewGuid())))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Company.CityNotUsable");
        var updated = await admin.PutJsonAsync(Company, Update(before.GetProperty("rowVersion").GetString()!, city.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var company = await _api.CreateClient().As("").GetJsonAsync(Company);
        Assert.Equal(("123456", "+20 2 2345 6789", city.GetProperty("nameAr").GetString(), "EG"),
            (company.GetProperty("commercialRegistrationNumber").GetString(), company.GetProperty("phone").GetString(),
                company.GetProperty("city").GetProperty("nameAr").GetString(), company.GetProperty("city").GetProperty("countryCode").GetString()));

        // Any signed-in user may print it, as they may read the company.
        var reader = _api.CreateClient().As("", warehouses: Guid.NewGuid().ToString());
        var response = await reader.GetAsync($"{Company}/profile?lang=Ar");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(@"^company-profile-\d{8}-\d{4}\.pdf$", response.Content.Headers.ContentDisposition?.FileNameStar);
        AssertPdfHas(await response.Content.ReadAsByteArrayAsync(), "ملف الشركة");
        AssertPdfHas(await reader.DownloadAsync($"{Company}/profile?lang=En", "application/pdf"), "Company profile");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync($"{Company}/profile")).StatusCode);
    }

    [Fact]
    public async Task Each_file_and_report_needs_the_read_permissions_of_what_it_lists()
    {
        var warehousesOnly = _api.CreateClient().As(OrganizationPermissions.WarehousesRead);
        var branchesOnly = _api.CreateClient().As(OrganizationPermissions.BranchesRead);

        Assert.Equal(HttpStatusCode.Forbidden, (await warehousesOnly.GetAsync($"{OrgExports}/branches")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchesOnly.GetAsync($"{OrgExports}/warehouses")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchesOnly.GetAsync($"{OrgExports}/locations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchesOnly.GetAsync(LocationDirectory)).StatusCode);
        foreach (var url in new[] { $"{OrgReports}/summary", $"{OrgReports}/structure", $"{OrgReports}/data-quality", $"{OrgExports}/structure", $"{OrgExports}/data-quality" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await warehousesOnly.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await branchesOnly.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync(url)).StatusCode);
        }
        await warehousesOnly.DownloadAsync($"{OrgExports}/locations?format=Csv&search=nothing-matches-this", "text/csv");
        var invalid = await (await Admin().GetAsync($"{OrgExports}/warehouses?format=7")).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("format", invalid.GetProperty("errors")[0].GetProperty("property").GetString());
    }

    [Fact]
    public async Task Too_many_rows_for_a_format_is_a_clear_refusal()
    {
        var admin = Admin();
        var (branchId, _) = await CreateBranchAsync(admin);
        var (warehouseId, _) = await CreateWarehouseAsync(admin, branchId);
        await CreateLocationAsync(admin, warehouseId, "X-1");
        await using var api = new ApiFactory(fx.ConnectionString, settings: new Dictionary<string, string?> { ["Documents:MaxPdfRows"] = "1" });
        var client = api.CreateClient().As(AllPermissions);

        await (await client.GetAsync($"{OrgExports}/locations?format=Pdf&warehouseId={warehouseId}"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Export.TooManyRows");
        await client.DownloadAsync($"{OrgExports}/locations?format=Xlsx&warehouseId={warehouseId}", Xlsx);
    }

    // Document metadata strings are written as UTF-16BE hex when they hold non-ASCII text, as plain text otherwise.
    private static void AssertPdfHas(byte[] pdf, string text)
    {
        var expected = text.All(char.IsAscii) ? text : "FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(text));
        Assert.Contains(expected, Encoding.Latin1.GetString(pdf), StringComparison.OrdinalIgnoreCase);
    }
}
