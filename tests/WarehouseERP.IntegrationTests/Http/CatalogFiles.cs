using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClosedXML.Excel;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>Builds catalog import files and reads downloaded ones, for the import/export API tests.</summary>
internal static class CatalogFiles
{
    public const string Imports = "/api/catalog/imports";
    public const string Exports = "/api/catalog/exports";
    public const string Reports = "/api/catalog/reports";

    /// <summary>A workbook with these sheets; each sheet's first row is its header. Strings become text cells,
    /// numbers numeric cells (as a user typing into Excel would produce).</summary>
    public static byte[] Workbook(params (string Sheet, object?[][] Rows)[] sheets)
    {
        using var workbook = new XLWorkbook();
        foreach (var (name, rows) in sheets)
        {
            var sheet = workbook.Worksheets.Add(name);
            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                {
                    var cell = sheet.Cell(r + 1, c + 1);
                    switch (rows[r][c])
                    {
                        case null:
                            break;
                        case string text:
                            cell.SetValue(text);
                            break;
                        default:
                            cell.Value = Convert.ToDouble(rows[r][c], System.Globalization.CultureInfo.InvariantCulture);
                            break;
                    }
                }
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static object?[] Row(params object?[] cells) => cells;

    public static readonly object?[] UnitsHeader = Row("Code", "Arabic name", "English name", "Allows fractions");
    public static readonly object?[] CategoriesHeader = Row("Code", "Arabic name", "English name", "Parent code");
    public static readonly object?[] ProductsHeader =
        Row("SKU", "Arabic name", "English name", "Base unit code", "Category code", "Product type", "Tracking");
    public static readonly object?[] ProductUnitsHeader = Row("SKU", "Unit code", "Conversion factor");
    public static readonly object?[] BarcodesHeader = Row("SKU", "Unit code", "Barcode", "Barcode type");

    public static Task<HttpResponseMessage> PreviewAsync(
        HttpClient client, byte[] file, string mode, string fileName = "catalog.xlsx", string? sheet = null) =>
        client.PostAsync($"{Imports}/preview", Form(file, fileName, mode, sheet, null));

    public static Task<HttpResponseMessage> CommitAsync(
        HttpClient client, byte[] file, string mode, string token, string fileName = "catalog.xlsx", string? sheet = null) =>
        client.PostAsync($"{Imports}/commit", Form(file, fileName, mode, sheet, token));

    /// <summary>Previews and asserts 200.</summary>
    public static async Task<JsonElement> PreviewOkAsync(HttpClient client, byte[] file, string mode, string fileName = "catalog.xlsx", string? sheet = null)
    {
        var response = await PreviewAsync(client, file, mode, fileName, sheet);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Preview: expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Previews, expects a committable preview, commits it and asserts 200.</summary>
    public static async Task<JsonElement> ImportAsync(HttpClient client, byte[] file, string mode = "Create")
    {
        var preview = await PreviewOkAsync(client, file, mode);
        Assert.True(preview.GetProperty("canCommit").GetBoolean(), $"Not committable: {preview}");
        var response = await CommitAsync(client, file, mode, preview.GetProperty("previewToken").GetString()!);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Commit: expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>The issues of a preview as (sheet, row, field, code).</summary>
    public static List<(string Sheet, int Row, string? Field, string Code)> Issues(JsonElement preview) =>
        preview.GetProperty("issues").EnumerateArray()
            .Select(i => (i.GetProperty("sheet").GetString()!, i.GetProperty("row").GetInt32(),
                i.GetProperty("field").ValueKind == JsonValueKind.Null ? null : i.GetProperty("field").GetString(),
                i.GetProperty("code").GetString()!))
            .ToList();

    /// <summary>The action of one row of a preview.</summary>
    public static string Action(JsonElement preview, string sheet, int row) =>
        preview.GetProperty("rows").EnumerateArray()
            .Single(r => r.GetProperty("sheet").GetString() == sheet && r.GetProperty("row").GetInt32() == row)
            .GetProperty("action").GetString()!;

    public static int Total(JsonElement result, string count) => result.GetProperty("totals").GetProperty(count).GetInt32();

    public static async Task<byte[]> DownloadAsync(this HttpClient client, string url, string expectedContentType)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET {url}: expected 200, got {(int)response.StatusCode}: {System.Text.Encoding.UTF8.GetString(body)}");
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.MediaType);
        return body;
    }

    public static XLWorkbook Read(byte[] xlsx) => new(new MemoryStream(xlsx));

    /// <summary>A GS1 number: the digits and their mod-10 check digit.</summary>
    public static string WithCheckDigit(string digits)
    {
        var sum = digits.Reverse().Select((c, i) => (c - '0') * (i % 2 == 0 ? 3 : 1)).Sum();
        return digits + (char)('0' + (10 - sum % 10) % 10);
    }

    private static MultipartFormDataContent Form(byte[] file, string fileName, string mode, string? sheet, string? token)
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var form = new MultipartFormDataContent { { content, "file", fileName }, { new StringContent(mode), "mode" } };
        if (sheet is not null)
            form.Add(new StringContent(sheet), "sheet");
        if (token is not null)
            form.Add(new StringContent(token), "previewToken");
        return form;
    }
}
