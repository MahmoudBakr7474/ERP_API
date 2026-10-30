using System.Globalization;
using System.Net;
using System.Text.Json;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

/// <summary>
/// Shared steps for the Accounting API tests. The collection's database has one company, one settings row (accounting
/// starts 2027-01-01) and one chart: the first test applies the default chart and creates fiscal year 2027, every test
/// adds its own accounts (unique codes under the default headers) and its own branches, so its figures are its own.
/// Tests that close periods use later years of their own.
/// </summary>
internal static class AccountingTesting
{
    public const string Settings = "/api/accounting/settings";
    public const string Accounts = "/api/accounting/accounts";
    public const string Mappings = "/api/accounting/account-mappings";
    public const string Years = "/api/accounting/fiscal-years";
    public const string Periods = "/api/accounting/fiscal-periods";
    public const string Journals = "/api/accounting/journals";
    public const string Reports = "/api/accounting/reports";
    public const string Vouchers = "/api/accounting/vouchers";
    public const string OpenItems = "/api/accounting/open-items";
    public const string Allocations = "/api/accounting/allocations";
    public const string CashAccounts = "/api/accounting/cash-accounts";
    public const string BankReconciliations = "/api/accounting/bank-reconciliations";

    public static readonly string AllPermissions = string.Join(',', AccountingPermissions.All);

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly HashSet<string> Prepared = [];

    /// <summary>The default chart and fiscal year 2027, once per database; the chart's account ids by code.</summary>
    public static async Task<IReadOnlyDictionary<string, Guid>> EnsureLedgerAsync(DatabaseFixture fx, HttpClient admin)
    {
        await fx.EnsureCompanyAsync();
        await Gate.WaitAsync();
        try
        {
            if (Prepared.Add(fx.ConnectionString))
            {
                var chart = await admin.PostAsync($"{Accounts}/default-chart", null);
                Assert.True(chart.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, await chart.Content.ReadAsStringAsync());
                await EnsureYearAsync(admin, 2027);
            }
        }
        finally
        {
            Gate.Release();
        }
        return await ChartAsync(admin);
    }

    public static async Task<IReadOnlyDictionary<string, Guid>> ChartAsync(HttpClient client)
    {
        var tree = await (await client.GetAsync($"{Accounts}/tree")).ReadAsync<JsonElement>();
        return tree.EnumerateArray().ToDictionary(a => a.Text("code")!, a => a.GetProperty("id").GetGuid());
    }

    /// <summary>Creates the missing fiscal years up to <paramref name="year"/>, one after the other.</summary>
    public static async Task EnsureYearAsync(HttpClient admin, int year)
    {
        var years = await (await admin.GetAsync(Years)).ReadAsync<JsonElement>();
        var last = years.EnumerateArray().Select(y => DateOnly.Parse(y.Text("startDate")!, CultureInfo.InvariantCulture).Year).DefaultIfEmpty(year - 1).Max();
        for (var next = last + 1; next <= year; next++)
        {
            var created = await admin.PostJsonAsync(Years, new { startYear = next });
            Assert.True(created.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict, await created.Content.ReadAsStringAsync());
        }
    }

    public static async Task<JsonElement> YearAsync(HttpClient client, int startYear)
    {
        var years = await (await client.GetAsync(Years)).ReadAsync<JsonElement>();
        return years.EnumerateArray().Single(y => DateOnly.Parse(y.Text("startDate")!, CultureInfo.InvariantCulture).Year == startYear);
    }

    public static async Task<Guid> PeriodIdAsync(HttpClient client, DateOnly date)
    {
        var year = await YearAsync(client, date.Year);
        return year.GetProperty("periods").EnumerateArray().Single(p => p.GetProperty("number").GetInt32() == date.Month).GetProperty("id").GetGuid();
    }

    /// <summary>A new posting account under a header of the default chart, with a code of its own.</summary>
    public static Task<Guid> CreateAccountAsync(
        HttpClient admin, IReadOnlyDictionary<string, Guid> chart, string parentCode, AccountType type, StatementSection section,
        bool manual = true, PartyType? party = null) =>
        admin.PostJsonAsync(Accounts, new
        {
            code = parentCode + Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString(CultureInfo.InvariantCulture),
            nameAr = "حساب اختبار", nameEn = "Test account", parentId = chart[parentCode], type, section, isPosting = true,
            allowsManualPosting = manual, partyType = party,
        }).CreatedIdAsync();

    public static object Line(Guid accountId, Guid branchId, decimal debit = 0m, decimal credit = 0m, string? description = null) =>
        new { accountId, branchId, debit, credit, description };

    public static Task<HttpResponseMessage> DraftAsync(HttpClient client, DateOnly date, params object[] lines) =>
        client.PostJsonAsync(Journals, new { postingDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), description = "قيد اختبار", reference = "T-1", lines });

    public static async Task<JsonElement> JournalAsync(HttpClient client, Guid journalId) =>
        await (await client.GetAsync($"{Journals}/{journalId}")).ReadAsync<JsonElement>();

    public static Task<HttpResponseMessage> ActAsync(HttpClient client, Guid journalId, string action, string rowVersion, Guid? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{Journals}/{journalId}/{action}")
        {
            Content = JsonContent(new { rowVersion }),
        };
        if (key is { } k)
            request.Headers.Add("Idempotency-Key", k.ToString());
        return client.SendAsync(request);
    }

    /// <summary>
    /// What posts this draft and the version to send with it: "post" when it needs no approval, otherwise it is submitted
    /// now and "approve" posts it.
    /// </summary>
    public static async Task<(string Action, string RowVersion)> ReadyToPostAsync(HttpClient admin, Guid journalId)
    {
        var journal = await JournalAsync(admin, journalId);
        var rowVersion = journal.Text("rowVersion")!;
        if (!journal.GetProperty("requiresApproval").GetBoolean())
            return ("post", rowVersion);
        var submitted = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{Journals}/{journalId}/submit")
        {
            Content = JsonContent(new { rowVersion }),
        });
        Assert.True(submitted.StatusCode == HttpStatusCode.OK, await submitted.Content.ReadAsStringAsync());
        return ("approve", (await submitted.ReadAsync<JsonElement>()).Text("rowVersion")!);
    }

    /// <summary>Posts a draft whatever the approval policy: directly, or submitted and approved by the same user.</summary>
    public static async Task<JsonElement> PostNowAsync(HttpClient admin, Guid journalId)
    {
        var (action, rowVersion) = await ReadyToPostAsync(admin, journalId);
        return await PostedAsync(ActAsync(admin, journalId, action, rowVersion, Guid.NewGuid()));
    }

    public static async Task<JsonElement> PostedAsync(Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Drafts and posts a balanced journal; its id and number.</summary>
    public static async Task<(Guid Id, string Number)> PostJournalAsync(HttpClient admin, DateOnly date, params object[] lines)
    {
        var id = await DraftAsync(admin, date, lines).CreatedIdAsync();
        var posted = await PostNowAsync(admin, id);
        return (id, posted.Text("number")!);
    }

    public static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A control-account line naming its customer or supplier (and when it falls due; its posting date otherwise).</summary>
    public static object PartyLine(
        Guid accountId, Guid branchId, PartyType partyType, Guid partyId, decimal debit = 0m, decimal credit = 0m, DateOnly? dueDate = null) =>
        new { accountId, branchId, debit, credit, description = (string?)null, partyType, partyId, dueDate = dueDate is { } d ? Day(d) : null };

    /// <summary>A cashbox or bank on a new ledger account of its own under 1204; its id and its ledger account's id.</summary>
    public static async Task<(Guid Id, Guid AccountId)> CreateCashAccountAsync(
        HttpClient admin, IReadOnlyDictionary<string, Guid> chart, Guid branchId, CashAccountKind kind, bool allowsOverdraft = false)
    {
        var account = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var bank = kind == CashAccountKind.Bank;
        var id = await admin.PostJsonAsync(CashAccounts, new
        {
            code = DatabaseFixture.Code(bank ? "BK" : "CB"), name = bank ? "بنك الاختبار" : "خزينة الاختبار", kind, branchId, accountId = account,
            bankName = bank ? "البنك الأهلي" : null, bankAccountNumber = bank ? "100200300" : null, allowsOverdraft,
        }).CreatedIdAsync();
        return (id, account);
    }

    public static object VoucherLine(Guid accountId, Guid branchId, decimal amount) => new { accountId, branchId, amount, description = (string?)null };

    public static object Allocate(Guid openItemId, decimal amount) => new { openItemId, amount };

    public static Task<HttpResponseMessage> VoucherDraftAsync(
        HttpClient client, JournalType type, DateOnly date, decimal amount = 0m, Guid? cashAccountId = null, Guid? counterCashAccountId = null,
        PartyType? partyType = null, Guid? partyId = null, PaymentMethod? method = null, decimal withholding = 0m, decimal tax = 0m,
        string? reference = null, object[]? lines = null, object[]? allocations = null) =>
        client.PostJsonAsync(Vouchers, new
        {
            type, postingDate = Day(date), description = "سند اختبار", reference, cashAccountId, counterCashAccountId, partyType, partyId,
            paymentMethod = method, amount, withholdingAmount = withholding, taxAmount = tax, dueDate = (string?)null, branchId = (Guid?)null,
            lines, allocations,
        });

    /// <summary>Drafts and posts a voucher; the journal's id.</summary>
    public static async Task<Guid> PostVoucherAsync(
        HttpClient admin, JournalType type, DateOnly date, decimal amount = 0m, Guid? cashAccountId = null, Guid? counterCashAccountId = null,
        PartyType? partyType = null, Guid? partyId = null, PaymentMethod? method = null, decimal withholding = 0m, decimal tax = 0m,
        string? reference = null, object[]? lines = null, object[]? allocations = null)
    {
        var id = await VoucherDraftAsync(admin, type, date, amount, cashAccountId, counterCashAccountId, partyType, partyId, method, withholding, tax,
            reference, lines, allocations).CreatedIdAsync();
        await PostNowAsync(admin, id);
        return id;
    }

    public static async Task<JsonElement[]> OpenItemsAsync(HttpClient client, PartyType partyType, Guid partyId, bool openOnly = false) =>
        (await (await client.GetAsync($"{OpenItems}?partyType={partyType}&partyId={partyId}&openOnly={openOnly}&pageSize=100")).ReadAsync<JsonElement>())
            .GetProperty("items").EnumerateArray().ToArray();

    /// <summary>The open item a journal opened (vouchers and hand journals here have one control-account line).</summary>
    public static JsonElement ItemOf(IEnumerable<JsonElement> items, Guid journalId) =>
        items.Single(i => i.GetProperty("journalId").GetGuid() == journalId);

    public static Guid IdOf(JsonElement element) => element.GetProperty("id").GetGuid();

    /// <summary>The journal's lines as (account, debit, credit, party), in line order.</summary>
    public static List<(Guid Account, decimal Debit, decimal Credit, Guid? Party)> LinesOf(JsonElement journal) =>
        journal.GetProperty("lines").EnumerateArray()
            .Select(l => (l.GetProperty("accountId").GetGuid(), l.Decimal("debit"), l.Decimal("credit"),
                l.GetProperty("partyId").ValueKind == JsonValueKind.Null ? (Guid?)null : l.GetProperty("partyId").GetGuid()))
            .ToList();

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, HttpTesting.Json), System.Text.Encoding.UTF8, "application/json");
}
