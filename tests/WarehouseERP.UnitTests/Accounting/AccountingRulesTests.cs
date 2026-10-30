using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.UnitTests.Accounting;

// Accounting rules without a database: the chart's shape (codes under parents, types, sections, control accounts), which
// accounts a posting role accepts, journals that balance by their own lines (never by an added line), approval and
// posting states, reversals that mirror and can't be edited, fiscal years of twelve periods that close and reopen in
// order, and the start date on the first day of a month.
public sealed class AccountingRulesTests
{
    private static readonly DateTime Now = new(2027, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Branch = Guid.NewGuid();

    // EF Core gives ids on add; these tests set them the same way.
    private static T WithId<T>(T entity, Type? declaring = null)
    {
        (declaring ?? typeof(T)).GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
        return entity;
    }

    private static Account Header(string code, AccountType type, Account? parent = null) =>
        WithId(Account.Create(code, "رأس", null, parent, type, null, false, false, null).Value, typeof(MasterEntity));

    private static Account Posting(string code, Account parent, StatementSection section, bool manual = true, PartyType? party = null) =>
        WithId(Account.Create(code, "حساب", null, parent, parent.Type, section, true, manual, party).Value, typeof(MasterEntity));

    private static JournalLineInput Debit(decimal amount, Guid? account = null) => new(account ?? Guid.NewGuid(), Branch, amount, 0m, null);

    private static JournalLineInput Credit(decimal amount, Guid? account = null) => new(account ?? Guid.NewGuid(), Branch, 0m, amount, null);

    private static JournalEntry Draft(bool requiresApproval = false, params JournalLineInput[] lines) =>
        JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 10), "قيد", null, "EGP", requiresApproval,
            lines.Length == 0 ? [Debit(100m), Credit(100m)] : lines, null, Now).Value;

    private static DocumentNumber Number(int sequence = 1) => new($"GJ-2027-{sequence:D6}", Guid.NewGuid(), sequence);

    [Fact]
    public void A_child_account_extends_its_parent_code_keeps_its_type_and_a_posting_account_reports_in_a_fitting_section()
    {
        var expenses = Header("5", AccountType.Expense);
        var admin = Header("53", AccountType.Expense, expenses);

        Assert.Equal((2, admin.ParentId), (admin.Level, (Guid?)expenses.Id));
        Assert.Equal(AccountDomainErrors.CodeOutsideParent, Account.Create("54", "x", null, admin, AccountType.Expense, StatementSection.OperatingExpenses, true, true, null).Error);
        Assert.Equal(AccountDomainErrors.CodeOutsideParent, Account.Create("53", "x", null, admin, AccountType.Expense, StatementSection.OperatingExpenses, true, true, null).Error);
        Assert.Equal(AccountDomainErrors.CodeInvalid, Account.Create("53A", "x", null, admin, AccountType.Expense, StatementSection.OperatingExpenses, true, true, null).Error);
        Assert.Equal(AccountDomainErrors.TypeDiffersFromParent, Account.Create("5301", "x", null, admin, AccountType.Revenue, StatementSection.Revenue, true, true, null).Error);
        Assert.Equal(AccountDomainErrors.SectionRequired, Account.Create("5301", "x", null, admin, AccountType.Expense, null, true, true, null).Error);
        Assert.Equal(AccountDomainErrors.SectionDoesNotFitType, Account.Create("5301", "x", null, admin, AccountType.Expense, StatementSection.Equity, true, true, null).Error);

        var rent = Posting("5302", admin, StatementSection.OperatingExpenses);
        Assert.Equal(AccountDomainErrors.ParentIsPosting, Account.Create("530201", "x", null, rent, AccountType.Expense, StatementSection.OperatingExpenses, true, true, null).Error);
        admin.Deactivate();
        Assert.Equal(AccountDomainErrors.ParentNotActive, Account.Create("5303", "x", null, admin, AccountType.Expense, StatementSection.OperatingExpenses, true, true, null).Error);
    }

    [Fact]
    public void Only_a_posting_account_takes_manual_journals_or_a_party_and_customers_sit_on_assets_suppliers_on_liabilities()
    {
        var header = Account.Create("1", "الأصول", null, null, AccountType.Asset, null, isPosting: false, allowsManualPosting: true, null).Value;
        Assert.False(header.AllowsManualPosting);
        var assets = Header("12", AccountType.Asset, WithId(header, typeof(MasterEntity)));

        Assert.Equal(PartyType.Customer, Posting("1202", assets, StatementSection.CurrentAssets, party: PartyType.Customer).PartyType);
        Assert.Equal(AccountDomainErrors.PartyInvalid,
            Account.Create("1203", "x", null, assets, AccountType.Asset, StatementSection.CurrentAssets, true, true, PartyType.Supplier).Error);
        Assert.Equal(AccountDomainErrors.PartyInvalid,
            Account.Create("1204", "x", null, assets, AccountType.Asset, null, false, false, PartyType.Customer).Error);
    }

    [Fact]
    public void A_posting_role_takes_only_an_active_posting_account_of_its_type_and_party()
    {
        var equity = Header("3", AccountType.Equity);
        var retained = Posting("33", equity, StatementSection.Equity);
        var assets = Header("12", AccountType.Asset);
        var customers = Posting("1202", assets, StatementSection.CurrentAssets, party: PartyType.Customer);
        var debtors = Posting("1203", assets, StatementSection.CurrentAssets);

        Assert.True(AccountMapping.Create(PostingRole.RetainedEarnings, retained).IsSuccess);
        Assert.True(AccountMapping.Create(PostingRole.AccountsReceivable, customers).IsSuccess);
        Assert.Equal("Accounting.Mapping.AccountDoesNotFitRole", AccountMapping.Create(PostingRole.AccountsReceivable, debtors).Error!.Code);
        Assert.Equal("Accounting.Mapping.AccountDoesNotFitRole", AccountMapping.Create(PostingRole.Inventory, customers).Error!.Code);
        Assert.Equal(AccountMappingDomainErrors.NotPostingAccount, AccountMapping.Create(PostingRole.RetainedEarnings, equity).Error);
        retained.Deactivate();
        Assert.Equal(AccountMappingDomainErrors.AccountNotActive, AccountMapping.Create(PostingRole.RetainedEarnings, retained).Error);
        Assert.All(Enum.GetValues<PostingRole>(), role => Assert.NotEmpty(PostingRoleRules.TypesOf(role)));
    }

    [Fact]
    public void A_journal_balances_by_its_own_lines_one_side_each_in_the_currency_minor_unit()
    {
        Assert.Equal("Accounting.Journal.Unbalanced",
            JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "EGP", false, [Debit(100m), Credit(99.99m)], null, Now).Error!.Code);
        Assert.Equal(JournalEntryDomainErrors.TooFewLines,
            JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "EGP", false, [Debit(0.01m)], null, Now).Error);
        Assert.Equal("Accounting.Journal.LineOneSide",
            JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "EGP", false,
                [new JournalLineInput(Guid.NewGuid(), Branch, 5m, 5m, null), Credit(0m)], null, Now).Error!.Code);
        Assert.Equal("Accounting.Journal.LineAmountScale",
            JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "EGP", false, [Debit(1.005m), Credit(1.005m)], null, Now).Error!.Code);
        Assert.True(JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "KWD", false, [Debit(1.005m), Credit(1.005m)], null, Now).IsSuccess);
        Assert.Equal("Accounting.Journal.LineIncomplete",
            JournalEntry.CreateDraft(JournalType.Manual, new DateOnly(2027, 3, 1), "x", null, "EGP", false,
                [new JournalLineInput(Guid.Empty, Branch, 1m, 0m, null), Credit(1m)], null, Now).Error!.Code);

        var journal = Draft(lines: [Debit(60m), Debit(40m), Credit(100m)]);
        Assert.Equal((100m, 100m, 3), (journal.TotalDebit, journal.TotalCredit, journal.Lines.Count));
        Assert.Equal([1, 2, 3], journal.Lines.Select(l => l.LineNumber).ToArray());
    }

    [Fact]
    public void Approval_decides_how_a_journal_posts_and_a_posted_journal_never_changes()
    {
        var direct = Draft();
        Assert.Equal(JournalEntryDomainErrors.ApprovalNotRequired, direct.Submit(null, Now).Error);
        Assert.True(direct.CanPost(byApproval: false).IsSuccess);
        direct.MarkPosted(Number(), Guid.NewGuid(), byApproval: false, null, Now);
        Assert.Equal((JournalStatus.Posted, "GJ-2027-000001"), (direct.Status, direct.Number));
        Assert.Equal(JournalEntryDomainErrors.NotDraft, direct.ReplaceDraft(direct.PostingDate, "y", null, null, null, Now).Error);
        Assert.Equal(JournalEntryDomainErrors.NotOpen, direct.Cancel(null, Now).Error);
        Assert.Throws<InvalidOperationException>(() => direct.MarkPosted(Number(2), Guid.NewGuid(), false, null, Now));

        var writer = Guid.NewGuid();
        var approved = Draft(requiresApproval: true);
        Assert.Equal(JournalEntryDomainErrors.ApprovalRequired, approved.CanPost(byApproval: false).Error);
        Assert.Equal(JournalEntryDomainErrors.NotSubmitted, approved.CanPost(byApproval: true).Error);
        Assert.True(approved.Submit(writer, Now).IsSuccess);
        Assert.Equal(JournalEntryDomainErrors.ReasonRequired, approved.Reject(" ", writer, Now).Error);
        Assert.True(approved.Reject("ناقص مستند", writer, Now).IsSuccess);
        Assert.Equal(JournalStatus.Draft, approved.Status);
        Assert.True(approved.Submit(writer, Now).IsSuccess);
        approved.MarkPosted(Number(), Guid.NewGuid(), byApproval: true, writer, Now); // the writer approves their own journal
        Assert.Equal((writer, writer), (approved.ApprovedByUserId, approved.PostedByUserId));
        Assert.Equal([JournalAction.Created, JournalAction.Submitted, JournalAction.Rejected, JournalAction.Submitted, JournalAction.Approved,
            JournalAction.Posted], approved.Events.Select(e => e.Action).ToArray());

        var opening = JournalEntry.CreateDraft(JournalType.OpeningBalance, new DateOnly(2027, 1, 1), "أرصدة", null, "EGP", requiresApproval: false,
            [Debit(5m), Credit(5m)], null, Now).Value;
        Assert.True(opening.RequiresApproval);
    }

    [Fact]
    public void A_reversal_mirrors_a_posted_journal_keeps_its_lines_fixed_and_is_never_reversed_itself()
    {
        var expense = Guid.NewGuid();
        var cash = Guid.NewGuid();
        var original = WithId(Draft(lines: [Debit(80m, expense), Credit(80m, cash)]));
        Assert.Equal(JournalEntryDomainErrors.NotPosted, JournalEntry.CreateReversal(original, new DateOnly(2027, 4, 1), null, null, false, null, Now).Error);
        original.MarkPosted(Number(), Guid.NewGuid(), false, null, Now);

        var reversal = JournalEntry.CreateReversal(original, new DateOnly(2027, 4, 1), null, null, requiresApproval: false, null, Now).Value;
        Assert.Equal((JournalType.Reversal, true, original.Id, "GJ-2027-000001"), (reversal.JournalType, reversal.LinesFixed,
            reversal.ReversalOfJournalId!.Value, reversal.Reference));
        Assert.Equal((expense, 0m, 80m), (reversal.Lines[0].AccountId, reversal.Lines[0].Debit, reversal.Lines[0].Credit));
        Assert.Equal((cash, 80m, 0m), (reversal.Lines[1].AccountId, reversal.Lines[1].Debit, reversal.Lines[1].Credit));
        Assert.Equal(JournalEntryDomainErrors.LinesFixed,
            reversal.ReplaceDraft(new DateOnly(2027, 4, 2), "عكس", null, [Debit(1m), Credit(1m)], null, Now).Error);
        Assert.True(reversal.ReplaceDraft(new DateOnly(2027, 4, 2), "عكس", null, null, null, Now).IsSuccess);

        reversal.MarkPosted(new DocumentNumber("RJ-2027-000001", Guid.NewGuid(), 1), Guid.NewGuid(), false, null, Now);
        Assert.Equal(JournalEntryDomainErrors.NotReversible, JournalEntry.CreateReversal(reversal, new DateOnly(2027, 5, 1), null, null, false, null, Now).Error);
        original.RecordReversed("RJ-2027-000001", null, Now);
        Assert.Equal((JournalStatus.Posted, 80m), (original.Status, original.TotalDebit));
    }

    [Fact]
    public void A_fiscal_year_has_twelve_monthly_periods_that_close_before_the_year_and_reopen_with_a_reason()
    {
        var calendar = FiscalYear.Create(new DateOnly(2027, 1, 1), null, Now).Value;
        Assert.Equal(("2027", new DateOnly(2027, 12, 31), 12), (calendar.Name, calendar.EndDate, calendar.Periods.Count));
        Assert.Equal((new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28)), (calendar.Period(2)!.StartDate, calendar.Period(2)!.EndDate));
        var july = FiscalYear.Create(new DateOnly(2026, 7, 1), null, Now).Value;
        Assert.Equal(("2026/2027", new DateOnly(2027, 6, 30), 7), (july.Name, july.EndDate, july.PeriodOf(new DateOnly(2027, 1, 15))!.Number));
        Assert.Equal(FiscalYearDomainErrors.StartNotMonthStart, FiscalYear.Create(new DateOnly(2027, 1, 2), null, Now).Error);

        foreach (var period in calendar.Periods)
            WithId(period);
        var march = calendar.Period(3)!;
        Assert.Equal(FiscalYearDomainErrors.PeriodsStillOpen, calendar.Close(null, null, null, Now).Error);
        Assert.True(calendar.ClosePeriod(march.Id, null, Now).IsSuccess);
        Assert.Equal(FiscalYearDomainErrors.PeriodNotOpen, calendar.ClosePeriod(march.Id, null, Now).Error);
        Assert.Equal(FiscalYearDomainErrors.ReasonRequired, calendar.ReopenPeriod(march.Id, "", null, Now).Error);
        Assert.True(calendar.ReopenPeriod(march.Id, "قيد متأخر", null, Now).IsSuccess);

        foreach (var period in calendar.Periods)
            calendar.ClosePeriod(period.Id, null, Now);
        Assert.True(calendar.Close(Guid.NewGuid(), "CJ-2027-000001", null, Now).IsSuccess);
        Assert.Equal(FiscalYearDomainErrors.YearClosed, calendar.ReopenPeriod(march.Id, "تعديل", null, Now).Error);
        Assert.True(calendar.Reopen("تسوية", null, Now).IsSuccess);
        Assert.Equal((FiscalYearStatus.Open, (Guid?)null, false), (calendar.Status, calendar.ClosingJournalId, march.IsOpen));
    }

    [Fact]
    public void The_accounting_start_date_is_the_first_day_of_a_month()
    {
        var settings = (AccountingSettings)Activator.CreateInstance(typeof(AccountingSettings), nonPublic: true)!;
        Assert.Equal(AccountingSettingsDomainErrors.CutoverNotPeriodStart, settings.Change(1, new DateOnly(2027, 1, 15), true, false).Error);
        Assert.Equal(AccountingSettingsDomainErrors.StartMonthInvalid, settings.Change(13, new DateOnly(2027, 1, 1), true, false).Error);
        Assert.True(settings.Change(7, new DateOnly(2027, 7, 1), false, true).IsSuccess);
        Assert.Equal((7, new DateOnly(2027, 7, 1), false, true),
            (settings.FiscalYearStartMonth, settings.CutoverDate, settings.ManualJournalsRequireApproval, settings.VouchersRequireApproval));
        Assert.Equal("GJ-2027-000042", JournalNumbers.Format(JournalType.Manual, 2027, 42));
        Assert.Equal("DJ-000123", JournalNumbers.DraftReference(123));
    }
}
