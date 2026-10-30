using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using static WarehouseERP.IntegrationTests.Http.AccountingTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Accounting Milestone A through the real HTTP pipeline on SQL Server: the chart and its rules, posting-role mappings,
// settings locks, fiscal periods, manual and opening-balance journals with approval (the writer may approve), idempotent
// posting, closed periods, reversals, the reports and their agreement, branch figures that add up to the company's, access,
// and the database's own guards on balance and immutability.
[Collection(DatabaseCollection.Name)]
public sealed class AccountingApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private static readonly DateOnly March = new(2027, 3, 10);
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin(Guid? user = null) => _api.CreateClient().As(AllPermissions, userId: user ?? Guid.NewGuid());

    private async Task<Guid> BranchAsync() => (await fx.SeedBranchAndUnitAsync()).BranchId;

    [Fact]
    public async Task A_balanced_journal_posts_under_its_number_after_its_writer_submits_and_approves_it_and_never_changes_again()
    {
        var writerId = Guid.NewGuid();
        var admin = Admin(writerId);
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var rent = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);

        var id = await DraftAsync(admin, March, Line(rent, branch, debit: 150m, description: "إيجار مارس"), Line(cash, branch, credit: 150m))
            .CreatedIdAsync();
        var draft = await JournalAsync(admin, id);
        Assert.Equal(("Draft", "Manual", true, (string?)null), (draft.Text("status"), draft.Text("journalType"),
            draft.GetProperty("requiresApproval").GetBoolean(), draft.Text("number")));
        Assert.StartsWith("DJ-", draft.Text("draftReference"));
        Assert.Equal((150m, 150m), (draft.Decimal("totalDebit"), draft.Decimal("totalCredit")));

        await (await ActAsync(admin, id, "post", draft.Text("rowVersion")!, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ApprovalRequired");
        var submitted = await (await ActAsync(admin, id, "submit", draft.Text("rowVersion")!)).ReadAsync<JsonElement>();

        // The approver is the writer: only the permission, the company-wide scope and the state decide.
        var approver = _api.CreateClient().As(AccountingPermissions.JournalsApprove + "," + AccountingPermissions.JournalsRead, userId: writerId);
        var posted = await PostedAsync(ActAsync(approver, id, "approve", submitted.Text("rowVersion")!, Guid.NewGuid()));
        Assert.Matches(@"^GJ-2027-\d{6}$", posted.Text("number"));

        var journal = await JournalAsync(admin, id);
        Assert.Equal(("Posted", writerId, writerId, writerId), (journal.Text("status"), journal.GetProperty("createdByUserId").GetGuid(),
            journal.GetProperty("approvedByUserId").GetGuid(), journal.GetProperty("postedByUserId").GetGuid()));
        Assert.Equal(("2027", 3), (journal.Text("fiscalYearName"), journal.GetProperty("fiscalPeriodNumber").GetInt32()));
        Assert.Equal(["Created", "Submitted", "Approved", "Posted"],
            journal.GetProperty("events").EnumerateArray().Select(e => e.Text("action")!).ToArray());

        await (await admin.PutJsonAsync($"{Journals}/{id}", new
            {
                postingDate = Day(March), description = "تعديل", reference = (string?)null, lines = (object[]?)null,
                rowVersion = journal.Text("rowVersion"),
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.NotDraft");
        await (await ActAsync(admin, id, "cancel", journal.Text("rowVersion")!))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.NotOpen");
    }

    [Fact]
    public async Task Unbalanced_or_misdirected_lines_are_refused_and_nothing_is_balanced_for_the_user()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);

        var unbalanced = await (await DraftAsync(admin, March, Line(expense, branch, debit: 100m), Line(cash, branch, credit: 99.99m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.Unbalanced");
        Assert.Contains("0.01", unbalanced.GetProperty("detail").GetString());
        await (await DraftAsync(admin, March, Line(expense, branch, debit: 10.005m), Line(cash, branch, credit: 10.005m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.LineAmountScale");
        await (await DraftAsync(admin, March, Line(expense, branch, debit: 5m, credit: 5m), Line(cash, branch, credit: 0m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.LineOneSide");
        await (await DraftAsync(admin, March, Line(chart["53"], branch, debit: 5m), Line(cash, branch, credit: 5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.AccountNotPosting");
        await (await DraftAsync(admin, March, Line(chart["120101"], branch, debit: 5m), Line(cash, branch, credit: 5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.AccountNotManual");
        await (await DraftAsync(admin, March, Line(chart["120201"], branch, debit: 5m), Line(cash, branch, credit: 5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PartyLinesNotSupported");
        await (await DraftAsync(admin, March, Line(expense, Guid.NewGuid(), debit: 5m), Line(cash, branch, credit: 5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.BranchNotFound");
        await (await DraftAsync(admin, new DateOnly(2026, 12, 31), Line(expense, branch, debit: 5m), Line(cash, branch, credit: 5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.BeforeCutover");
        await (await DraftAsync(admin, March, Line(expense, branch, debit: 5m)))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");

        // A draft may be dated in a year nobody has created yet; it can't post there.
        var journal = await DraftAsync(admin, new DateOnly(2045, 5, 1), Line(expense, branch, debit: 5m), Line(cash, branch, credit: 5m))
            .CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, journal);
        await (await ActAsync(admin, journal, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PeriodMissing");
    }

    [Fact]
    public async Task A_closed_period_refuses_postings_until_it_is_reopened_with_a_reason()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        await EnsureYearAsync(admin, 2028);
        var branch = await BranchAsync();
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var february = new DateOnly(2028, 2, 15);
        var id = await DraftAsync(admin, february, Line(expense, branch, debit: 70m), Line(cash, branch, credit: 70m)).CreatedIdAsync();
        var period = await PeriodIdAsync(admin, february);

        var closed = await (await admin.PostAsync($"{Periods}/{period}/close", null)).ReadAsync<JsonElement>();
        Assert.True(closed.GetProperty("openJournals").GetInt32() >= 1);
        await (await admin.PostAsync($"{Periods}/{period}/close", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.FiscalPeriod.NotOpen");

        var (action, rowVersion) = await ReadyToPostAsync(admin, id);
        await (await ActAsync(admin, id, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PeriodClosed");
        Assert.Equal(action == "approve" ? "Submitted" : "Draft", (await JournalAsync(admin, id)).Text("status"));

        var noReopen = _api.CreateClient().As(AccountingPermissions.PeriodsManage);
        Assert.Equal(HttpStatusCode.Forbidden, (await noReopen.PostJsonAsync($"{Periods}/{period}/reopen", new { reason = "x" })).StatusCode);
        await (await admin.PostJsonAsync($"{Periods}/{period}/reopen", new { reason = " " }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostJsonAsync($"{Periods}/{period}/reopen", new { reason = "تصحيح قيد" })).StatusCode);

        var posted = await PostedAsync(ActAsync(admin, id, action, rowVersion, Guid.NewGuid()));
        Assert.Matches(@"^GJ-2028-\d{6}$", posted.Text("number"));
        var february2028 = (await YearAsync(admin, 2028)).GetProperty("events").EnumerateArray()
            .Where(e => e.GetProperty("periodNumber").ValueKind == JsonValueKind.Number && e.GetProperty("periodNumber").GetInt32() == 2)
            .ToList();
        Assert.Equal(["PeriodClosed", "PeriodReopened"], february2028.Select(e => e.Text("action")!).ToArray());
        Assert.Equal("تصحيح قيد", february2028[1].Text("note"));
    }

    [Fact]
    public async Task A_close_racing_a_posting_lets_the_posting_in_only_if_it_committed_first()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        await EnsureYearAsync(admin, 2028);
        var branch = await BranchAsync();
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var march = new DateOnly(2028, 3, 20);

        for (var round = 0; round < 3; round++)
        {
            var id = await DraftAsync(admin, march, Line(expense, branch, debit: 9m), Line(cash, branch, credit: 9m)).CreatedIdAsync();
            var (action, rowVersion) = await ReadyToPostAsync(admin, id);
            var period = await PeriodIdAsync(admin, march);
            var responses = await Task.WhenAll(
                ActAsync(admin, id, action, rowVersion, Guid.NewGuid()),
                admin.PostAsync($"{Periods}/{period}/close", null));
            Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
            var status = (await JournalAsync(admin, id)).Text("status");
            if (responses[0].StatusCode == HttpStatusCode.OK)
                Assert.Equal("Posted", status);
            else
            {
                await responses[0].AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PeriodClosed");
                Assert.NotEqual("Posted", status);
            }
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PostJsonAsync($"{Periods}/{period}/reopen", new { reason = "جولة اختبار" })).StatusCode);
        }
    }

    [Fact]
    public async Task Retrying_a_posting_with_its_key_posts_once_and_the_key_cannot_serve_another_request()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var id = await DraftAsync(admin, March, Line(expense, branch, debit: 33.33m), Line(cash, branch, credit: 33.33m)).CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, id);

        await (await ActAsync(admin, id, action, rowVersion)).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var key = Guid.NewGuid();
        var attempts = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => ActAsync(admin, id, action, rowVersion, key)));
        var numbers = new List<string>();
        foreach (var attempt in attempts)
            numbers.Add((await PostedAsync(Task.FromResult(attempt))).Text("number")!);
        Assert.Single(numbers.Distinct());
        var retried = await PostedAsync(ActAsync(admin, id, action, rowVersion, key));
        Assert.Equal(numbers[0], retried.Text("number"));

        var other = await DraftAsync(admin, March, Line(expense, branch, debit: 1m), Line(cash, branch, credit: 1m)).CreatedIdAsync();
        var (otherAction, otherRowVersion) = await ReadyToPostAsync(admin, other);
        await (await ActAsync(admin, other, otherAction, otherRowVersion, key))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Idempotency.KeyReused");

        await using var db = fx.NewDb();
        Assert.Equal(1, await db.Set<JournalEntry>().CountAsync(x => x.Number == numbers[0]));
        Assert.Equal(1, await db.Set<IdempotencyRecord>().CountAsync(x => x.Key == key));
    }

    [Fact]
    public async Task A_reversal_turns_the_lines_around_on_its_own_date_once_and_leaves_the_original_as_posted()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var (originalId, originalNumber) = await PostJournalAsync(admin, new DateOnly(2027, 5, 5),
            Line(expense, branch, debit: 80m), Line(cash, branch, credit: 80m));
        var before = await JournalAsync(admin, originalId);

        await (await admin.PostJsonAsync($"{Journals}/{originalId}/reverse", new { postingDate = "2027-05-04" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ReversalBeforeOriginal");
        var reversalId = await admin.PostJsonAsync($"{Journals}/{originalId}/reverse", new { postingDate = "2027-05-20" }).CreatedIdAsync();
        await (await admin.PostJsonAsync($"{Journals}/{originalId}/reverse", new { postingDate = "2027-05-21" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Journal.AlreadyReversed");

        var reversal = await JournalAsync(admin, reversalId);
        Assert.Equal(("Reversal", true, originalId, originalNumber), (reversal.Text("journalType"),
            reversal.GetProperty("linesFixed").GetBoolean(), reversal.GetProperty("reversalOfJournalId").GetGuid(), reversal.Text("reversalOfNumber")));
        var lines = reversal.GetProperty("lines").EnumerateArray().ToList();
        Assert.Equal((0m, 80m), (lines[0].Decimal("debit"), lines[0].Decimal("credit")));
        Assert.Equal((80m, 0m), (lines[1].Decimal("debit"), lines[1].Decimal("credit")));
        await (await admin.PutJsonAsync($"{Journals}/{reversalId}", new
            {
                postingDate = "2027-05-20", description = "عكس", reference = (string?)null,
                lines = new[] { Line(expense, branch, debit: 1m), Line(cash, branch, credit: 1m) }, rowVersion = reversal.Text("rowVersion"),
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.LinesFixed");

        var posted = await PostNowAsync(admin, reversalId);
        Assert.Matches(@"^RJ-2027-\d{6}$", posted.Text("number"));
        var after = await JournalAsync(admin, originalId);
        Assert.Equal(("Posted", before.Decimal("totalDebit"), reversalId), (after.Text("status"), after.Decimal("totalDebit"),
            after.GetProperty("reversedByJournalId").GetGuid()));
        Assert.Equal(posted.Text("number"), after.GetProperty("events").EnumerateArray().Last().Text("note"));
        Assert.Equal("Reversed", after.GetProperty("events").EnumerateArray().Last().Text("action"));
        await (await admin.PostJsonAsync($"{Journals}/{reversalId}/reverse", new { postingDate = "2027-05-25" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.NotReversible");

        var statement = await (await admin.GetAsync($"{Reports}/account-statement?accountId={cash}&fromDate=2027-05-01&toDate=2027-05-31"))
            .ReadAsync<JsonElement>();
        Assert.Equal((0m, 80m, 80m, 0m), (statement.Decimal("openingBalance"), statement.Decimal("totalDebit"), statement.Decimal("totalCredit"),
            statement.Decimal("closingBalance")));
    }

    [Fact]
    public async Task Trial_balance_balance_sheet_and_income_statement_agree_and_branches_add_up_to_the_company()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var (north, south) = (await BranchAsync(), await BranchAsync());
        var revenue = await CreateAccountAsync(admin, chart, "41", AccountType.Revenue, StatementSection.Revenue);
        var expense = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var cash = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        var april = (From: "2027-04-01", To: "2027-04-30");
        await PostJournalAsync(admin, new DateOnly(2027, 4, 2), Line(cash, north, debit: 1000m), Line(revenue, north, credit: 1000m));
        await PostJournalAsync(admin, new DateOnly(2027, 4, 9), Line(expense, south, debit: 300m), Line(cash, north, credit: 300m));
        await PostJournalAsync(admin, new DateOnly(2027, 4, 15), Line(expense, north, debit: 200m), Line(cash, north, credit: 200m));

        var trial = await (await admin.GetAsync($"{Reports}/trial-balance?fromDate={april.From}&toDate={april.To}")).ReadAsync<JsonElement>();
        Assert.True(trial.GetProperty("isBalanced").GetBoolean());
        JsonElement Row(JsonElement report, Guid account) =>
            report.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("accountId").GetGuid() == account);
        Assert.Equal((0m, 0m, 1000m, 500m, 500m, 0m), (Row(trial, cash).Decimal("openingDebit"), Row(trial, cash).Decimal("openingCredit"),
            Row(trial, cash).Decimal("periodDebit"), Row(trial, cash).Decimal("periodCredit"), Row(trial, cash).Decimal("closingDebit"),
            Row(trial, cash).Decimal("closingCredit")));
        Assert.Equal((500m, 1000m), (Row(trial, expense).Decimal("closingDebit"), Row(trial, revenue).Decimal("closingCredit")));
        Assert.True(Row(trial, chart["1204"]).Decimal("periodDebit") >= 1000m); // the header sums its posting accounts

        var northTrial = await (await admin.GetAsync($"{Reports}/trial-balance?fromDate={april.From}&toDate={april.To}&branchId={north}"))
            .ReadAsync<JsonElement>();
        Assert.False(northTrial.GetProperty("isBalanced").GetBoolean()); // the 9 April journal spans two branches
        Assert.Equal((1000m, 500m), (Row(northTrial, cash).Decimal("periodDebit"), Row(northTrial, cash).Decimal("periodCredit")));

        async Task<JsonElement> IncomeAsync(Guid? branch) => await (await admin.GetAsync(
            $"{Reports}/income-statement?fromDate={april.From}&toDate={april.To}" + (branch is { } b ? $"&branchId={b}" : ""))).ReadAsync<JsonElement>();
        static decimal Amount(JsonElement statement, Guid account) =>
            statement.GetProperty("lines").EnumerateArray().Where(l => l.GetProperty("accountId").GetGuid() == account).Sum(l => l.Decimal("amount"));
        var (company, northIncome, southIncome) = (await IncomeAsync(null), await IncomeAsync(north), await IncomeAsync(south));
        Assert.Equal((1000m, 500m), (Amount(company, revenue), Amount(company, expense)));
        Assert.Equal((1000m, 200m, 0m, 300m), (Amount(northIncome, revenue), Amount(northIncome, expense), Amount(southIncome, revenue),
            Amount(southIncome, expense)));
        Assert.Equal(Amount(company, expense), Amount(northIncome, expense) + Amount(southIncome, expense));
        Assert.Equal(Row(trial, revenue).Decimal("closingCredit"), Amount(company, revenue));

        var sheet = await (await admin.GetAsync($"{Reports}/balance-sheet?asOfDate={april.To}")).ReadAsync<JsonElement>();
        Assert.True(sheet.GetProperty("isBalanced").GetBoolean());
        Assert.Equal(sheet.Decimal("totalAssets"), sheet.Decimal("totalLiabilitiesAndEquity"));
        Assert.Equal(500m, sheet.GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("accountId").GetGuid() == cash).Decimal("amount"));

        var statement = await (await admin.GetAsync($"{Reports}/account-statement?accountId={cash}&fromDate={april.From}&toDate={april.To}"))
            .ReadAsync<JsonElement>();
        Assert.Equal([1000m, 700m, 500m], statement.GetProperty("lines").EnumerateArray().Select(l => l.Decimal("balance")).ToArray());
        Assert.Equal(Row(trial, cash).Decimal("closingDebit"), statement.Decimal("closingBalance"));

        var ledger = await (await admin.GetAsync($"{Reports}/general-ledger?fromDate={april.From}&toDate={april.To}&accountId={cash}&pageSize=2"))
            .ReadAsync<JsonElement>();
        Assert.Equal((3, 2), (ledger.GetProperty("totalCount").GetInt32(), ledger.GetProperty("items").GetArrayLength()));
    }

    [Fact]
    public async Task Opening_balances_are_dated_on_the_start_date_always_approved_and_may_use_system_accounts()
    {
        var writerId = Guid.NewGuid();
        var admin = Admin(writerId);
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var lines = new[] { Line(chart["120101"], branch, debit: 5000m), Line(chart["35"], branch, credit: 5000m) };

        var journalsOnly = _api.CreateClient().As(AccountingPermissions.JournalsManage + "," + AccountingPermissions.JournalsRead);
        Assert.Equal(HttpStatusCode.Forbidden, (await journalsOnly.PostJsonAsync($"{Journals}/opening", new { description = "x", lines })).StatusCode);
        await (await admin.PostJsonAsync($"{Journals}/opening", new
            {
                description = "أرصدة", lines = new[] { Line(chart["120201"], branch, debit: 1m), Line(chart["35"], branch, credit: 1m) },
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PartyLinesNotSupported");

        var id = await admin.PostJsonAsync($"{Journals}/opening", new { description = "الأرصدة الافتتاحية", reference = "OB", lines }).CreatedIdAsync();
        var draft = await JournalAsync(admin, id);
        Assert.Equal(("OpeningBalance", "2027-01-01", true), (draft.Text("journalType"), draft.Text("postingDate"),
            draft.GetProperty("requiresApproval").GetBoolean()));
        await (await ActAsync(journalsOnly, id, "submit", draft.Text("rowVersion")!))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.PermissionDenied");
        await (await ActAsync(admin, id, "post", draft.Text("rowVersion")!, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ApprovalRequired");
        var posted = await PostNowAsync(admin, id);
        Assert.Matches(@"^OB-2027-\d{6}$", posted.Text("number"));
    }

    [Fact]
    public async Task Accounting_needs_company_wide_access_and_the_permission_of_each_action()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: Guid.NewGuid().ToString());
        foreach (var url in new[] { Accounts, $"{Accounts}/tree", Years, Journals, Mappings, $"{Reports}/balance-sheet?asOfDate=2027-01-31" })
            await (await scoped.GetAsync(url)).AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.CompanyWideRequired");
        await (await DraftAsync(scoped, March, Line(chart["5302"], branch, debit: 1m), Line(chart["120401"], branch, credit: 1m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.CompanyWideRequired");

        var reader = _api.CreateClient().As(AccountingPermissions.JournalsRead + "," + AccountingPermissions.AccountsRead);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync(Journals)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await DraftAsync(reader, March, Line(chart["5302"], branch, debit: 1m),
            Line(chart["120401"], branch, credit: 1m))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync($"{Reports}/balance-sheet?asOfDate=2027-01-31")).StatusCode);

        var writer = _api.CreateClient().As(AccountingPermissions.JournalsManage + "," + AccountingPermissions.JournalsRead);
        var id = await DraftAsync(writer, March, Line(chart["5302"], branch, debit: 2m), Line(chart["120401"], branch, credit: 2m)).CreatedIdAsync();
        var draft = await JournalAsync(writer, id);
        Assert.Equal(HttpStatusCode.Forbidden, (await ActAsync(writer, id, "approve", draft.Text("rowVersion")!, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await writer.PostAsync($"{Accounts}/default-chart", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await writer.PostJsonAsync(Years, new { startYear = 2040 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await writer.PutJsonAsync(Settings, new
        {
            fiscalYearStartMonth = 1, cutoverDate = "2027-01-01", manualJournalsRequireApproval = true, rowVersion = "AAAAAAAAAAA=",
        })).StatusCode);
    }

    [Fact]
    public async Task Settings_fix_the_start_month_once_years_exist_and_the_start_date_once_a_journal_posted()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        await PostJournalAsync(admin, March, Line(chart["5302"], branch, debit: 3m), Line(chart["120401"], branch, credit: 3m));
        var settings = await (await admin.GetAsync(Settings)).ReadAsync<JsonElement>();
        Assert.Equal((1, "2027-01-01", "EGP", true, true), (settings.GetProperty("fiscalYearStartMonth").GetInt32(), settings.Text("cutoverDate"),
            settings.Text("currencyCode"), settings.GetProperty("startMonthLocked").GetBoolean(), settings.GetProperty("cutoverLocked").GetBoolean()));
        var approval = settings.GetProperty("manualJournalsRequireApproval").GetBoolean();
        var rowVersion = settings.Text("rowVersion");

        await (await admin.PutJsonAsync(Settings, new { fiscalYearStartMonth = 7, cutoverDate = "2027-01-01", manualJournalsRequireApproval = approval, rowVersion }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Settings.StartMonthLocked");
        await (await admin.PutJsonAsync(Settings, new { fiscalYearStartMonth = 1, cutoverDate = "2027-02-01", manualJournalsRequireApproval = approval, rowVersion }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Settings.CutoverLocked");
        await (await admin.PutJsonAsync(Settings, new { fiscalYearStartMonth = 1, cutoverDate = "2027-01-15", manualJournalsRequireApproval = approval, rowVersion }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");

        // The approval policy may change at any time; a journal drafted without it posts directly.
        var changed = await (await admin.PutJsonAsync(Settings, new { fiscalYearStartMonth = 1, cutoverDate = "2027-01-01", manualJournalsRequireApproval = false, rowVersion }))
            .ReadAsync<JsonElement>();
        try
        {
            var id = await DraftAsync(admin, March, Line(chart["5302"], branch, debit: 4m), Line(chart["120401"], branch, credit: 4m)).CreatedIdAsync();
            var draft = await JournalAsync(admin, id);
            Assert.False(draft.GetProperty("requiresApproval").GetBoolean());
            await (await ActAsync(admin, id, "submit", draft.Text("rowVersion")!))
                .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ApprovalNotRequired");
            await PostedAsync(ActAsync(admin, id, "post", draft.Text("rowVersion")!, Guid.NewGuid()));
        }
        finally
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.PutJsonAsync(Settings, new
            {
                fiscalYearStartMonth = 1, cutoverDate = "2027-01-01", manualJournalsRequireApproval = approval, rowVersion = changed.Text("rowVersion"),
            })).StatusCode);
        }
    }

    [Fact]
    public async Task The_chart_keeps_codes_under_their_parents_and_used_accounts_are_deactivated_not_deleted()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        object Account(string code, string parent, AccountType type, StatementSection? section, bool posting = true) =>
            new { code, nameAr = "حساب", nameEn = (string?)null, parentId = chart[parent], type, section, isPosting = posting, allowsManualPosting = true, partyType = (PartyType?)null };

        await (await admin.PostJsonAsync(Accounts, Account("9999", "53", AccountType.Expense, StatementSection.OperatingExpenses)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.CodeOutsideParent");
        await (await admin.PostJsonAsync(Accounts, Account("5398765", "53", AccountType.Revenue, StatementSection.Revenue)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.TypeDiffersFromParent");
        await (await admin.PostJsonAsync(Accounts, Account("5398766", "53", AccountType.Expense, null)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.SectionRequired");
        await (await admin.PostJsonAsync(Accounts, Account("5398767", "53", AccountType.Expense, StatementSection.CurrentAssets)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.SectionDoesNotFitType");
        await (await admin.PostJsonAsync(Accounts, Account("5302", "53", AccountType.Expense, StatementSection.OperatingExpenses)))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Account.CodeTaken");
        await (await admin.PostJsonAsync(Accounts, Account("530299", "5302", AccountType.Expense, StatementSection.OperatingExpenses)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.ParentIsPosting");

        var unused = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Accounts}/{unused}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Accounts}/{unused}")).StatusCode);

        var used = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        await PostJournalAsync(admin, March, Line(used, branch, debit: 12m), Line(chart["120401"], branch, credit: 12m));
        await (await admin.DeleteAsync($"{Accounts}/{used}")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.HasLines");
        await (await admin.PostAsync($"{Accounts}/{used}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.HasBalance");
        await (await admin.PostAsync($"{Accounts}/{chart["33"]}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.Mapped");
        await (await admin.PostAsync($"{Accounts}/{chart["53"]}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.HasActiveChildren");

        var details = await (await admin.GetAsync($"{Accounts}/{used}")).ReadAsync<JsonElement>();
        Assert.True(details.GetProperty("hasLines").GetBoolean());
        var renamed = await admin.PutJsonAsync($"{Accounts}/{used}", new
        {
            nameAr = "مصروف معاد تسميته", nameEn = "Renamed", section = StatementSection.OtherExpenses, allowsManualPosting = true,
            rowVersion = details.Text("rowVersion"),
        });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        await (await admin.PutJsonAsync($"{Accounts}/{used}", new
            {
                nameAr = "x", nameEn = (string?)null, section = StatementSection.OtherExpenses, allowsManualPosting = true, rowVersion = details.Text("rowVersion"),
            }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        await (await admin.PostAsync($"{Accounts}/default-chart", null)).AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Chart.NotEmpty");
        await (await admin.PostJsonAsync($"{Accounts}/import", new
            {
                accounts = new[] { new { code = "1", nameAr = "الأصول", nameEn = (string?)null, parentCode = (string?)null, type = AccountType.Asset, section = (StatementSection?)null, isPosting = false, allowsManualPosting = false, partyType = (PartyType?)null } },
                roles = (object[]?)null,
            }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Chart.NotEmpty");
    }

    [Fact]
    public async Task Every_posting_role_is_listed_and_maps_only_to_a_fitting_active_posting_account()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var mappings = await (await admin.GetAsync(Mappings)).ReadAsync<JsonElement>();
        Assert.Equal(Enum.GetValues<PostingRole>().Length, mappings.GetArrayLength());
        Assert.All(mappings.EnumerateArray(), m => Assert.NotEqual(JsonValueKind.Null, m.GetProperty("accountId").ValueKind));
        var receivables = mappings.EnumerateArray().Single(m => m.Text("role") == "AccountsReceivable");
        Assert.Equal(("120201", "Customer"), (receivables.Text("accountCode"), receivables.Text("requiredParty")));

        await (await admin.PutJsonAsync($"{Mappings}/AccountsReceivable", new { accountId = chart["120204"], rowVersion = receivables.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Mapping.AccountDoesNotFitRole");
        await (await admin.PutJsonAsync($"{Mappings}/Inventory", new { accountId = chart["12"], rowVersion = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed"); // mapped already: its version is needed

        var equity = await CreateAccountAsync(admin, chart, "3", AccountType.Equity, StatementSection.Equity);
        var retained = mappings.EnumerateArray().Single(m => m.Text("role") == "RetainedEarnings");
        var moved = await (await admin.PutJsonAsync($"{Mappings}/RetainedEarnings", new { accountId = equity, rowVersion = retained.Text("rowVersion") }))
            .ReadAsync<JsonElement>();
        await (await admin.PostAsync($"{Accounts}/{equity}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Account.Mapped");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutJsonAsync($"{Mappings}/RetainedEarnings",
            new { accountId = chart["33"], rowVersion = moved.Text("rowVersion") })).StatusCode);

        await using var db = fx.NewDb();
        var history = await db.Set<AccountMapping>().TemporalAll().Where(x => x.Role == PostingRole.RetainedEarnings).Select(x => x.AccountId).ToListAsync();
        Assert.Contains(equity, history);
        Assert.Equal(chart["33"], (await db.Set<AccountMapping>().SingleAsync(x => x.Role == PostingRole.RetainedEarnings)).AccountId);
    }

    [Fact]
    public async Task The_database_refuses_unbalanced_one_sided_or_unnumbered_rows_even_bypassing_the_domain()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var (id, _) = await PostJournalAsync(admin, March, Line(chart["5302"], branch, debit: 6m), Line(chart["120401"], branch, credit: 6m));
        await using var db = fx.NewDb();
        await DbAssert.SqlRejectedBy(db, $"UPDATE accounting.JournalEntries SET TotalCredit = 5 WHERE Id = {id}", "CK_JournalEntries_Balanced");
        await DbAssert.SqlRejectedBy(db, $"UPDATE accounting.JournalEntries SET Number = NULL WHERE Id = {id}", "CK_JournalEntries_Posted");
        await DbAssert.SqlRejectedBy(db, $"UPDATE accounting.JournalLines SET Credit = 1 WHERE JournalEntryId = {id} AND LineNumber = 1",
            "CK_JournalLines_OneSide");
        await DbAssert.SqlRejectedBy(db, $"UPDATE accounting.JournalLines SET Debit = -6 WHERE JournalEntryId = {id} AND LineNumber = 1",
            "CK_JournalLines_OneSide");
    }
}
