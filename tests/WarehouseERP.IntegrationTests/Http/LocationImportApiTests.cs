using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogFiles;
using static WarehouseERP.IntegrationTests.Http.OrganizationTesting;

namespace WarehouseERP.IntegrationTests.Http;

// The storage location import against SQL Server and the real host: preview changes nothing, commit is all or nothing
// and only what was previewed, modes decide what happens to existing locations, and every rule of the single-location
// endpoints holds for imported rows; a location never moves, never changes its code, and the default one is never touched.
[Collection(DatabaseCollection.Name)]
public sealed class LocationImportApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private static readonly object?[] Header = Row("Branch code", "Warehouse code", "Location code", "Arabic name", "English name");
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        await fx.EnsureCompanyAsync();
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private static byte[] File(params object?[][] rows) => Workbook(("Locations", [Header, .. rows]));

    private static Task<HttpResponseMessage> PreviewAsync(HttpClient client, byte[] file, string mode, string fileName = "locations.xlsx") =>
        client.PostAsync($"{LocationImports}/preview", Form(file, fileName, mode, null));

    private static Task<HttpResponseMessage> CommitAsync(HttpClient client, byte[] file, string mode, string token, string fileName = "locations.xlsx") =>
        client.PostAsync($"{LocationImports}/commit", Form(file, fileName, mode, token));

    private static async Task<JsonElement> PreviewOkAsync(HttpClient client, byte[] file, string mode, string fileName = "locations.xlsx")
    {
        var response = await PreviewAsync(client, file, mode, fileName);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Preview: expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task<JsonElement> ImportAsync(HttpClient client, byte[] file, string mode = "Create", string fileName = "locations.xlsx")
    {
        var preview = await PreviewOkAsync(client, file, mode, fileName);
        Assert.True(preview.GetProperty("canCommit").GetBoolean(), $"Not committable: {preview}");
        var response = await CommitAsync(client, file, mode, preview.GetProperty("previewToken").GetString()!, fileName);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Commit: expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static List<(int Row, string? Field, string Code)> IssuesOf(JsonElement preview) =>
        preview.GetProperty("issues").EnumerateArray()
            .Select(i => (i.GetProperty("row").GetInt32(),
                i.GetProperty("field").ValueKind == JsonValueKind.Null ? null : i.GetProperty("field").GetString(), i.GetProperty("code").GetString()!))
            .ToList();

    private static string ActionOf(JsonElement preview, int row) =>
        preview.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("row").GetInt32() == row).GetProperty("action").GetString()!;

    private async Task<List<JsonElement>> LocationsAsync(Guid warehouseId) =>
        (await Admin().GetJsonAsync($"{LocationDirectory}?warehouseId={warehouseId}&pageSize=100")).Items().ToList();

    private async Task<(Guid BranchId, string BranchCode, Guid WarehouseId, string WarehouseCode)> WarehouseAsync()
    {
        var admin = Admin();
        var (branchId, branchCode) = await CreateBranchAsync(admin);
        var (warehouseId, warehouseCode) = await CreateWarehouseAsync(admin, branchId);
        return (branchId, branchCode, warehouseId, warehouseCode);
    }

    [Fact]
    public async Task Create_then_update_and_skip_go_through_preview_and_commit()
    {
        var (_, branch, warehouseId, warehouse) = await WarehouseAsync();
        var client = Admin();

        // Codes compare without regard to case and lower-case branch/warehouse codes still find them.
        var created = await ImportAsync(client, File(
            Row(branch, warehouse, "A.01-01", "ممر أ رف 1", "Aisle A rack 1"),
            Row(branch.ToLowerInvariant(), warehouse.ToLowerInvariant(), "A.01-02", "ممر أ رف 2", null)));
        Assert.Equal((2, 2, 0), (Total(created, "rows"), Total(created, "create"), Total(created, "invalid")));
        var locations = await LocationsAsync(warehouseId);
        Assert.Equal(["DEFAULT", "A.01-01", "A.01-02"], locations.Select(l => l.GetProperty("code").GetString()));
        Assert.All(locations, l => Assert.True(l.GetProperty("isActive").GetBoolean()));

        var preview = await PreviewOkAsync(client, File(
            Row(branch, warehouse, "a.01-01", "ممر أ رف 1 (معدل)", null), // renamed; the English name stays
            Row(branch, warehouse, "A.01-02", "ممر أ رف 2", null), // the same: unchanged
            Row(branch, warehouse, "A.01-03", "ممر أ رف 3", "Aisle A rack 3")), "Update");
        Assert.Equal(("Update", "Unchanged", "Create"), (ActionOf(preview, 2), ActionOf(preview, 3), ActionOf(preview, 4)));
        Assert.Equal(3, (await LocationsAsync(warehouseId)).Count); // a preview changes nothing
        var updated = await ImportAsync(client, File(
            Row(branch, warehouse, "a.01-01", "ممر أ رف 1 (معدل)", null),
            Row(branch, warehouse, "A.01-02", "ممر أ رف 2", null),
            Row(branch, warehouse, "A.01-03", "ممر أ رف 3", "Aisle A rack 3")), "Update");
        Assert.Equal((1, 1, 1), (Total(updated, "create"), Total(updated, "update"), Total(updated, "unchanged")));
        var renamed = (await LocationsAsync(warehouseId)).Single(l => l.GetProperty("code").GetString() == "A.01-01");
        Assert.Equal(("ممر أ رف 1 (معدل)", "Aisle A rack 1"), (renamed.GetProperty("nameAr").GetString(), renamed.GetProperty("nameEn").GetString()));

        var skipFile = File(Row(branch, warehouse, "A.01-01", "اسم آخر", null));
        var skipped = await PreviewOkAsync(client, skipFile, "Skip");
        Assert.Equal(("Skip", false), (ActionOf(skipped, 2), skipped.GetProperty("canCommit").GetBoolean()));
        await (await CommitAsync(client, skipFile, "Skip", skipped.GetProperty("previewToken").GetString()!))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Import.NothingToImport");
    }

    [Fact]
    public async Task Every_problem_is_reported_by_row_and_column_and_nothing_is_imported()
    {
        var admin = Admin();
        var (branchId, branch, warehouseId, warehouse) = await WarehouseAsync();
        var (transitId, transit) = await CreateWarehouseAsync(admin, branchId, WarehouseType.Transit);
        var (closedId, closed) = await CreateWarehouseAsync(admin, branchId);
        await admin.PostAsync($"{Warehouses}/{closedId}/deactivate", null).AssertNoContentAsync();
        var file = File(
            Row(branch, warehouse, "OK-1", "صالح", null),                       // 2 valid
            Row("NO-SUCH-BRANCH", warehouse, "X-1", "أ", null),                 // 3 branch
            Row(branch, "NO-SUCH-WH", "X-2", "أ", null),                        // 4 warehouse
            Row(branch, transit, "X-3", "أ", null),                             // 5 transit
            Row(branch, closed, "X-4", "أ", null),                              // 6 inactive warehouse
            Row(branch, warehouse, "رف 1", "أ", null),                          // 7 code format
            Row(branch, warehouse, "X-5", new string('أ', 151), null),         // 8 too long
            Row(branch, warehouse, "DUP", "أ", null),                           // 9 duplicate
            Row(branch, warehouse, "dup", "ب", null),                           // 10 duplicate (case)
            Row(branch, warehouse, "DEFAULT", "أ", null),                       // 11 the default already exists (Create)
            Row(null, warehouse, "X-6", null, null));                           // 12 required
        var preview = await PreviewOkAsync(admin, file, "Create");

        var issues = IssuesOf(preview);
        Assert.Contains((3, "branchCode", "Organization.Branch.NotFound"), issues);
        Assert.Contains((4, "warehouseCode", "Organization.Warehouse.NotFound"), issues);
        Assert.Contains((5, "warehouseCode", "Organization.Import.TransitWarehouse"), issues);
        Assert.Contains((6, "warehouseCode", "Organization.Warehouse.NotActive"), issues);
        Assert.Contains((7, "code", "Organization.Import.InvalidFormat"), issues);
        Assert.Contains((8, "nameAr", "Organization.Import.TooLong"), issues);
        Assert.Contains((9, "code", "Organization.Import.DuplicateInFile"), issues);
        Assert.Contains((10, "code", "Organization.Import.DuplicateInFile"), issues);
        Assert.Contains((11, "code", "Organization.WarehouseLocation.CodeTaken"), issues);
        Assert.Contains((12, "branchCode", "Organization.Import.Required"), issues);
        Assert.Contains((12, "nameAr", "Organization.Import.Required"), issues);
        Assert.DoesNotContain(issues, i => i.Row == 2);
        Assert.Equal("Create", ActionOf(preview, 2));
        Assert.False(preview.GetProperty("canCommit").GetBoolean());
        var totals = preview.GetProperty("totals");
        Assert.Equal((11, 1, 10, 2), (totals.GetProperty("rows").GetInt32(), totals.GetProperty("valid").GetInt32(),
            totals.GetProperty("invalid").GetInt32(), totals.GetProperty("duplicates").GetInt32()));

        await (await CommitAsync(admin, file, "Create", preview.GetProperty("previewToken").GetString()!))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Import.HasErrors");
        Assert.Single(await LocationsAsync(warehouseId)); // only DEFAULT: the valid row was not imported either
        Assert.Single(await LocationsAsync(transitId));

        // The error report holds every issue, in the user's language, to fix the file from.
        using var workbook = Read(await (await admin.PostAsync($"{LocationImports}/error-report?lang=Ar", Form(file, "locations.xlsx", "Create", null)))
            .Content.ReadAsByteArrayAsync());
        var sheet = workbook.Worksheets.First();
        Assert.Equal(issues.Count + 1, sheet.LastRowUsed()!.RowNumber());
        Assert.Contains(sheet.RowsUsed(), r => r.Cell(4).GetString() == "مواقع مخزن الترانزيت تفتحها وتغلقها التحويلات المخزنية فقط.");
    }

    [Fact]
    public async Task Create_mode_refuses_existing_locations_and_no_mode_changes_the_default()
    {
        var (_, branch, warehouseId, warehouse) = await WarehouseAsync();
        var client = Admin();
        await CreateLocationAsync(client, warehouseId, "EXISTS", "قائم", "Existing");

        var create = await PreviewOkAsync(client, File(Row(branch, warehouse, "exists", "قائم", null)), "Create");
        Assert.Contains((2, "code", "Organization.WarehouseLocation.CodeTaken"), IssuesOf(create));

        var rename = await PreviewOkAsync(client, File(Row(branch, warehouse, "DEFAULT", "اسم جديد للافتراضي", null)), "Update");
        Assert.Contains((2, "code", "Organization.Import.DefaultLocationFixed"), IssuesOf(rename));
        var same = await PreviewOkAsync(client, File(Row(branch, warehouse, "DEFAULT", "الموقع الافتراضي", null)), "Update");
        Assert.Equal("Unchanged", ActionOf(same, 2));
        var skip = await PreviewOkAsync(client, File(Row(branch, warehouse, "DEFAULT", "اسم جديد للافتراضي", null)), "Skip");
        Assert.Equal("Skip", ActionOf(skip, 2));

        var defaultLocation = (await LocationsAsync(warehouseId)).Single(l => l.GetProperty("isDefault").GetBoolean());
        Assert.Equal("الموقع الافتراضي", defaultLocation.GetProperty("nameAr").GetString());
    }

    [Fact]
    public async Task A_change_after_the_preview_and_a_second_confirmation_change_nothing()
    {
        var (_, branch, warehouseId, warehouse) = await WarehouseAsync();
        var client = Admin();
        var file = File(Row(branch, warehouse, "C-1", "أ", null), Row(branch, warehouse, "C-2", "ب", null));
        var token = (await PreviewOkAsync(client, file, "Create")).GetProperty("previewToken").GetString()!;

        // Someone adds C-2 on the screen between the preview and the confirmation.
        await CreateLocationAsync(client, warehouseId, "C-2");
        await (await CommitAsync(client, file, "Create", token)).AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Import.ChangedSincePreview");
        Assert.DoesNotContain(await LocationsAsync(warehouseId), l => l.GetProperty("code").GetString() == "C-1");

        // A different file, mode or token is refused too; the confirmed one imports once.
        var fresh = File(Row(branch, warehouse, "C-1", "أ", null));
        var freshToken = (await PreviewOkAsync(client, fresh, "Create")).GetProperty("previewToken").GetString()!;
        await (await CommitAsync(client, fresh, "Skip", freshToken)).AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Import.ChangedSincePreview");
        await (await CommitAsync(client, file, "Create", freshToken)).AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Import.ChangedSincePreview");
        Assert.Equal(HttpStatusCode.OK, (await CommitAsync(client, fresh, "Create", freshToken)).StatusCode);
        await (await CommitAsync(client, fresh, "Create", freshToken)).AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Import.ChangedSincePreview");
        Assert.Single(await LocationsAsync(warehouseId), l => l.GetProperty("code").GetString() == "C-1");
    }

    [Fact]
    public async Task A_failure_while_saving_rolls_the_whole_import_back()
    {
        var (_, branch, warehouseId, warehouse) = await WarehouseAsync();
        var client = Admin();
        var poisoned = $"FAIL-{Guid.NewGuid():N}"[..30];
        var trigger = $"TR_Test_{Guid.NewGuid():N}";
        var file = File(Row(branch, warehouse, "R-1", "أ", null), Row(branch, warehouse, "R-2", "ب", null), Row(branch, warehouse, poisoned, "ج", null));
        var token = (await PreviewOkAsync(client, file, "Create")).GetProperty("previewToken").GetString()!;

        var create = $"CREATE TRIGGER organization.[{trigger}] ON organization.WarehouseLocations AFTER INSERT AS " +
                     $"IF EXISTS (SELECT 1 FROM inserted WHERE Code = '{poisoned}') THROW 50001, 'Refused by the test.', 1;";
        var drop = $"DROP TRIGGER organization.[{trigger}];";
        await using (var db = fx.NewDb())
            await db.Database.ExecuteSqlRawAsync(create);
        try
        {
            Assert.Equal(HttpStatusCode.InternalServerError, (await CommitAsync(client, file, "Create", token)).StatusCode);
        }
        finally
        {
            await using var db = fx.NewDb();
            await db.Database.ExecuteSqlRawAsync(drop);
        }
        Assert.Single(await LocationsAsync(warehouseId)); // only DEFAULT
    }

    [Fact]
    public async Task Rows_follow_the_users_scope_and_permissions()
    {
        var admin = Admin();
        var (branchId, branch, mine, mineCode) = await WarehouseAsync();
        var (other, otherCode) = await CreateWarehouseAsync(admin, branchId);
        await CreateLocationAsync(admin, mine, "OLD", "قديم", null);
        var file = File(Row(branch, mineCode, "NEW", "جديد", null), Row(branch, otherCode, "NEW", "جديد", null),
            Row(branch, "NOT-THERE", "NEW", "جديد", null), Row(branch, mineCode, "OLD", "قديم جدًا", null));

        // Scoped to one warehouse: the other one, and an unknown code, read the same.
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: mine.ToString());
        var preview = await PreviewOkAsync(scoped, file, "Update");
        Assert.Equal(("Create", "Update"), (ActionOf(preview, 2), ActionOf(preview, 5)));
        Assert.Contains((3, "warehouseCode", "Organization.Warehouse.AccessDenied"), IssuesOf(preview));
        Assert.Contains((4, "warehouseCode", "Organization.Warehouse.AccessDenied"), IssuesOf(preview));

        // Each row needs the permission of what it does.
        var createOnly = _api.CreateClient().As($"{OrganizationPermissions.WarehousesRead},{OrganizationPermissions.LocationsCreate}");
        var renames = await PreviewOkAsync(createOnly, File(Row(branch, mineCode, "OLD", "قديم جدًا", null)), "Update");
        Assert.Contains((2, null, "Organization.Import.NotPermitted"), IssuesOf(renames));
        var readOnly = _api.CreateClient().As(OrganizationPermissions.WarehousesRead);
        await (await PreviewAsync(readOnly, file, "Create")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Import.NotPermitted");
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.CreateClient().As(OrganizationPermissions.LocationsCreate).PostAsync($"{LocationImports}/preview",
            Form(file, "locations.xlsx", "Create", null))).StatusCode);
        Assert.DoesNotContain(await LocationsAsync(other), l => l.GetProperty("code").GetString() == "NEW");
    }

    [Fact]
    public async Task The_template_lists_the_users_warehouses_and_csv_files_import_too()
    {
        var (_, branch, warehouseId, warehouse) = await WarehouseAsync();
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: warehouseId.ToString());

        using (var template = Read(await scoped.DownloadAsync($"{LocationImports}/template?lang=Ar", Xlsx)))
        {
            Assert.Equal(["التعليمات", "المواقع", "القوائم"], template.Worksheets.Select(w => w.Name));
            var data = template.Worksheet("المواقع");
            Assert.Equal(("كود الفرع *", "الاسم الإنجليزي"), (data.Cell(1, 1).GetString(), data.Cell(1, 5).GetString()));
            Assert.Equal(1, data.LastRowUsed()!.RowNumber()); // no example rows: an untouched template imports nothing
            var lists = template.Worksheet("القوائم");
            Assert.Equal(2, lists.LastRowUsed()!.RowNumber()); // the one warehouse in scope
            Assert.Equal((branch, warehouse), (lists.Cell(2, 1).GetString(), lists.Cell(2, 3).GetString()));
        }
        var untouched = await PreviewOkAsync(scoped, await scoped.DownloadAsync($"{LocationImports}/template?lang=En", Xlsx), "Create");
        Assert.Equal([(1, null, "Organization.Import.NoRows")], IssuesOf(untouched));

        // A CSV with Arabic headers and a semicolon separator, as Excel writes it in some locales.
        var csv = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(
            $"كود الفرع;كود المخزن;كود الموقع;الاسم العربي\r\n{branch};{warehouse};CSV-1;رف من ملف\r\n")).ToArray();
        var result = await ImportAsync(scoped, csv, fileName: "locations.csv");
        Assert.Equal(1, Total(result, "create"));
        Assert.Contains(await LocationsAsync(warehouseId), l => l.GetProperty("code").GetString() == "CSV-1");

        var missing = await PreviewOkAsync(scoped, Workbook(("Locations", [Row("Branch code", "Location code", "Arabic name")])), "Create");
        Assert.Contains((1, "warehouseCode", "Organization.Import.MissingColumn"), IssuesOf(missing));
        await (await PreviewAsync(scoped, "not a workbook"u8.ToArray(), "Create", "locations.txt"))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var noMode = new MultipartFormDataContent { { new ByteArrayContent(csv), "file", "locations.csv" } };
        var missingMode = await (await scoped.PostAsync($"{LocationImports}/preview", noMode)).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("mode", missingMode.GetProperty("errors")[0].GetProperty("property").GetString());
    }

    private static MultipartFormDataContent Form(byte[] file, string fileName, string mode, string? token)
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var form = new MultipartFormDataContent { { content, "file", fileName }, { new StringContent(mode), "mode" } };
        if (token is not null)
            form.Add(new StringContent(token), "previewToken");
        return form;
    }
}
