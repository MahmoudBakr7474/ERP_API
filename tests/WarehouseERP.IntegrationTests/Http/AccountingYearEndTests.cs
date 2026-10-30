using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using static WarehouseERP.IntegrationTests.Http.AccountingTesting;

namespace WarehouseERP.IntegrationTests.Http;

// A chart is imported or defaulted only into an empty chart, and a year closes only after every earlier year: both need a
// ledger nobody else uses, so this class migrates its own database (like CompanySetupTests). One test, one extra database.
public sealed class AccountingYearEndTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db = new();
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _api = new ApiFactory(_db.ConnectionString, clock: _db.Clock);
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _db.DisposeAsync();
    }

    private static object Row(string code, string? parent, AccountType type, StatementSection? section, bool posting = true) =>
        new { code, nameAr = "حساب", nameEn = (string?)null, parentCode = parent, type, section, isPosting = posting, allowsManualPosting = posting, partyType = (PartyType?)null };

    [Fact]
    public async Task The_default_chart_fills_an_empty_ledger_and_a_closed_year_moves_each_branch_result_into_retained_earnings_until_it_reopens()
    {
        await _db.EnsureCompanyAsync();
        var (north, south) = ((await _db.SeedBranchAndUnitAsync()).BranchId, (await _db.SeedBranchAndUnitAsync()).BranchId);
        var admin = _api.CreateClient().As(AllPermissions, userId: Guid.NewGuid());

        // An import is all or nothing and keeps the domain's rules; nothing is created by a refused one.
        await (await admin.PostJsonAsync($"{Accounts}/import", new { accounts = new[] { Row("1", null, AccountType.Asset, null, false), Row("1", null, AccountType.Asset, null, false) } }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Chart.CodeDuplicate");
        await (await admin.PostJsonAsync($"{Accounts}/import", new { accounts = new[] { Row("1101", "11", AccountType.Asset, StatementSection.CurrentAssets) } }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Chart.ParentMissing");
        await (await admin.PostJsonAsync($"{Accounts}/import", new
            {
                accounts = new[] { Row("3", null, AccountType.Equity, null, false), Row("31", "3", AccountType.Equity, StatementSection.Revenue) },
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.SectionDoesNotFitType");
        Assert.Empty(await ChartAsync(admin));

        var applied = await (await admin.PostAsync($"{Accounts}/default-chart", null)).ReadAsync<JsonElement>();
        Assert.Equal(Enum.GetValues<PostingRole>().Length, applied.GetProperty("rolesMapped").GetInt32());
        var chart = await ChartAsync(admin);
        Assert.Equal(applied.GetProperty("accountsCreated").GetInt32(), chart.Count);
        await (await admin.PostAsync($"{Accounts}/default-chart", null)).AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Chart.NotEmpty");

        // Years follow one another.
        await admin.PostJsonAsync(Years, new { startYear = 2027 }).CreatedIdAsync();
        await (await admin.PostJsonAsync(Years, new { startYear = 2029 })).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.FiscalYear.NotContiguous");
        await (await admin.PostJsonAsync(Years, new { startYear = 2027 })).AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.FiscalYear.Exists");
        await admin.PostJsonAsync(Years, new { startYear = 2028 }).CreatedIdAsync();
        var year = await YearAsync(admin, 2027);
        Assert.Equal(12, year.GetProperty("periods").GetArrayLength());
        Assert.Equal(("2027", "2027-12-31"), (year.Text("name"), year.Text("endDate")));

        // North sells 1000; south pays rent of 400 out of north's cashbox: the company earns 600, north 1000, south -400.
        await PostJournalAsync(admin, new DateOnly(2027, 6, 1), Line(chart["120401"], north, debit: 1000m), Line(chart["4101"], north, credit: 1000m));
        await PostJournalAsync(admin, new DateOnly(2027, 9, 1), Line(chart["5302"], south, debit: 400m), Line(chart["120401"], north, credit: 400m));
        await PostJournalAsync(admin, new DateOnly(2028, 1, 5), Line(chart["5302"], north, debit: 50m), Line(chart["120401"], north, credit: 50m));

        var yearId = year.GetProperty("id").GetGuid();
        await (await admin.PostJsonAsync($"{Years}/{yearId}/close", new { rowVersion = year.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.FiscalYear.PeriodsStillOpen");
        foreach (var period in year.GetProperty("periods").EnumerateArray())
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{Periods}/{period.GetProperty("id").GetGuid()}/close", null)).StatusCode);
        var year2028 = await YearAsync(admin, 2028);
        foreach (var period in year2028.GetProperty("periods").EnumerateArray())
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{Periods}/{period.GetProperty("id").GetGuid()}/close", null)).StatusCode);
        await (await admin.PostJsonAsync($"{Years}/{year2028.GetProperty("id").GetGuid()}/close", new { rowVersion = year2028.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.FiscalYear.EarlierYearOpen");

        var closed = await PostedAsync(admin.PostJsonAsync($"{Years}/{yearId}/close", new { rowVersion = year.Text("rowVersion") }));
        Assert.Equal((600m, "CJ-2027-000001"), (closed.Decimal("netProfit"), closed.Text("closingJournalNumber")));
        var closing = await JournalAsync(admin, closed.GetProperty("closingJournalId").GetGuid());
        Assert.Equal(("YearEndClosing", "Posted", "2027-12-31"), (closing.Text("journalType"), closing.Text("status"), closing.Text("postingDate")));
        var lines = closing.GetProperty("lines").EnumerateArray().Select(l => (Account: l.GetProperty("accountId").GetGuid(),
            Branch: l.GetProperty("branchId").GetGuid(), Debit: l.Decimal("debit"), Credit: l.Decimal("credit"))).ToList();
        Assert.Contains((chart["4101"], north, 1000m, 0m), lines);
        Assert.Contains((chart["5302"], south, 0m, 400m), lines);
        Assert.Contains((chart["33"], north, 0m, 1000m), lines);
        Assert.Contains((chart["33"], south, 400m, 0m), lines);
        Assert.Equal(4, lines.Count);

        // The closed year still shows what it earned; its balance sheet carries the result in retained earnings.
        var income = await (await admin.GetAsync($"{Reports}/income-statement?fromDate=2027-01-01&toDate=2027-12-31")).ReadAsync<JsonElement>();
        Assert.Equal(600m, income.GetProperty("totals").Decimal("netProfit"));
        var sheet = await (await admin.GetAsync($"{Reports}/balance-sheet?asOfDate=2027-12-31")).ReadAsync<JsonElement>();
        Assert.True(sheet.GetProperty("isBalanced").GetBoolean());
        Assert.Equal((600m, 0m, 600m), (sheet.Decimal("totalAssets"), sheet.Decimal("unclosedEarnings"),
            sheet.GetProperty("lines").EnumerateArray().Single(l => l.Text("code") == "33").Decimal("amount")));
        var trial = await (await admin.GetAsync($"{Reports}/trial-balance?fromDate=2028-01-01&toDate=2028-12-31")).ReadAsync<JsonElement>();
        Assert.True(trial.GetProperty("isBalanced").GetBoolean());
        Assert.DoesNotContain(trial.GetProperty("rows").EnumerateArray(), r => r.Text("code") == "4101");
        Assert.Equal(600m, trial.GetProperty("rows").EnumerateArray().Single(r => r.Text("code") == "33").Decimal("openingCredit"));

        // Nothing posts into the closed year, and its periods stay shut until the year reopens.
        var late = await DraftAsync(admin, new DateOnly(2027, 12, 20), Line(chart["5302"], north, debit: 5m), Line(chart["120401"], north, credit: 5m))
            .CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, late);
        await (await ActAsync(admin, late, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PeriodClosed");
        var december = year.GetProperty("periods")[11].GetProperty("id").GetGuid();
        await (await admin.PostJsonAsync($"{Periods}/{december}/reopen", new { reason = "تصحيح" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.FiscalYear.Closed");

        var closedYear = await YearAsync(admin, 2027);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostJsonAsync($"{Years}/{yearId}/reopen",
            new { reason = "قيد تسوية متأخر", rowVersion = closedYear.Text("rowVersion") })).StatusCode);
        var reopened = await YearAsync(admin, 2027);
        Assert.Equal(("Open", JsonValueKind.Null), (reopened.Text("status"), reopened.GetProperty("closingJournalId").ValueKind));
        var reversed = await JournalAsync(admin, closed.GetProperty("closingJournalId").GetGuid());
        Assert.Equal(("RJ-2027-000001", "Posted"), (reversed.Text("reversedByNumber"), reversed.Text("reversedByStatus")));
        sheet = await (await admin.GetAsync($"{Reports}/balance-sheet?asOfDate=2027-12-31")).ReadAsync<JsonElement>();
        Assert.Equal((600m, true), (sheet.Decimal("unclosedEarnings"), sheet.GetProperty("isBalanced").GetBoolean()));
        Assert.DoesNotContain(sheet.GetProperty("lines").EnumerateArray(), l => l.Text("code") == "33");

        var again = await PostedAsync(admin.PostJsonAsync($"{Years}/{yearId}/close", new { rowVersion = reopened.Text("rowVersion") }));
        Assert.Equal((600m, "CJ-2027-000002"), (again.Decimal("netProfit"), again.Text("closingJournalNumber")));
        Assert.Equal(["Created", "Closed", "Reopened", "Closed"], (await YearAsync(admin, 2027)).GetProperty("events").EnumerateArray()
            .Select(e => e.Text("action")!).Where(a => a != "PeriodClosed").ToArray());

        await using var db = _db.NewDb();
        Assert.All(await db.Set<JournalEntry>().ToListAsync(), j => Assert.Equal(j.TotalDebit, j.TotalCredit));
        var totals = await db.Set<JournalLine>().GroupBy(_ => 1).Select(g => new { Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) }).SingleAsync();
        Assert.Equal(totals.Debit, totals.Credit);
    }
}
