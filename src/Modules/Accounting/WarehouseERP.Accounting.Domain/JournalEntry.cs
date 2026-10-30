using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// A journal: balanced debit and credit lines on one posting date, in the base currency. Drafted, optionally submitted
/// and approved (whoever wrote it may approve it), then posted under its number into the open period of its date; a
/// posted journal never changes. It is corrected by a reversal journal (lines turned the other way, on its own date) or
/// by another journal. Year-end closing journals are written and posted by the system. Every step is a
/// <see cref="JournalEvent"/>.
/// </summary>
public sealed class JournalEntry : IAuditable
{
    public const int MaxLines = 500;

    /// <summary>A generated journal (a year-end close) has one line per account and branch, so it may be longer.</summary>
    public const int MaxGeneratedLines = 20_000;

    public const int MaxDescriptionLength = 500;
    public const int MaxReferenceLength = 100;
    public const int MaxNoteLength = 500;

    private readonly List<JournalLine> _lines = [];
    private readonly List<JournalEvent> _events = [];
    private readonly List<VoucherAllocation> _allocations = [];

    private JournalEntry() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>From an identity column; shown as DJ-000123 until the journal posts.</summary>
    public int DraftNumber { get; private set; }

    /// <summary>The final number (GJ-2027-000001), set when it posts.</summary>
    public string? Number { get; private set; }

    public Guid? FiscalYearId { get; private set; }
    public Guid? FiscalPeriodId { get; private set; }
    public int? SequenceNumber { get; private set; }
    public JournalType JournalType { get; private set; }
    public JournalStatus Status { get; private set; }

    /// <summary>The accounting date: the period it posts into.</summary>
    public DateOnly PostingDate { get; private set; }

    public string Description { get; private set; } = null!;

    /// <summary>An outside reference (a voucher or document number).</summary>
    public string? Reference { get; private set; }

    public string CurrencyCode { get; private set; } = null!;
    public bool RequiresApproval { get; private set; }

    /// <summary>True for a reversal: its lines mirror the reversed journal and can't be edited.</summary>
    public bool LinesFixed { get; private set; }

    /// <summary>The journal this one reverses.</summary>
    public Guid? ReversalOfJournalId { get; private set; }

    /// <summary>A voucher's cashbox or bank account (receipts, payments, refunds, cash vouchers, transfers).</summary>
    public Guid? CashAccountId { get; private set; }

    /// <summary>A transfer's destination cashbox or bank account.</summary>
    public Guid? CounterCashAccountId { get; private set; }

    /// <summary>A party document's customer or supplier (receipts, payments, refunds, expense bills).</summary>
    public PartyType? PartyType { get; private set; }

    public Guid? PartyId { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }

    /// <summary>
    /// A voucher's total: what settles the party (cash plus tax withheld) for receipts and payments, the money moved for
    /// refunds, cash vouchers and transfers, the bill's total with VAT for an expense bill.
    /// </summary>
    public decimal? VoucherAmount { get; private set; }

    /// <summary>Tax withheld on a receipt (by the customer) or a payment (by the company).</summary>
    public decimal? WithholdingAmount { get; private set; }

    /// <summary>Recoverable VAT on an expense bill.</summary>
    public decimal? TaxAmount { get; private set; }

    /// <summary>When an expense bill falls due.</summary>
    public DateOnly? DueDate { get; private set; }

    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<JournalLine> Lines => _lines;
    public IReadOnlyList<JournalEvent> Events => _events;

    /// <summary>The open items a receipt, payment or refund settles when it posts.</summary>
    public IReadOnlyList<VoucherAllocation> Allocations => _allocations;

    public bool IsVoucher => IsVoucherType(JournalType);

    public static bool IsVoucherType(JournalType type) => type is >= JournalType.CustomerReceipt and <= JournalType.ExpenseBill;

    public bool IsPosted => Status == JournalStatus.Posted;

    /// <summary>A manual or opening-balance draft. Opening balances always need an approver.</summary>
    public static Result<JournalEntry> CreateDraft(
        JournalType type, DateOnly postingDate, string description, string? reference, string currencyCode, bool requiresApproval,
        IReadOnlyList<JournalLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        if (type is not (JournalType.Manual or JournalType.OpeningBalance))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Only manual and opening-balance journals are drafted by hand.");
        var journal = new JournalEntry
        {
            JournalType = type,
            Status = JournalStatus.Draft,
            CurrencyCode = currencyCode,
            RequiresApproval = type == JournalType.OpeningBalance || requiresApproval,
        };
        var filled = journal.Fill(postingDate, description, reference, lines);
        if (filled.IsFailure)
            return filled.Error;
        journal.Record(JournalAction.Created, userId, nowUtc, null);
        return journal;
    }

    /// <summary>
    /// A draft reversal of a posted manual or opening-balance journal: the same lines turned the other way, dated on its own.
    /// Its lines can't change; it posts like a manual journal.
    /// </summary>
    public static Result<JournalEntry> CreateReversal(
        JournalEntry original, DateOnly postingDate, string? description, string? reference, bool requiresApproval, Guid? userId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (!original.IsPosted)
            return JournalEntryDomainErrors.NotPosted;
        if (original.JournalType is not (JournalType.Manual or JournalType.OpeningBalance) && !original.IsVoucher)
            return JournalEntryDomainErrors.NotReversible;
        var journal = new JournalEntry
        {
            JournalType = JournalType.Reversal,
            Status = JournalStatus.Draft,
            CurrencyCode = original.CurrencyCode,
            RequiresApproval = requiresApproval,
            LinesFixed = true,
            ReversalOfJournalId = original.Id,
        };
        var filled = journal.Fill(postingDate, Text.Optional(description) ?? $"عكس القيد {original.Number}",
            Text.Optional(reference) ?? original.Number, original.Mirrored());
        if (filled.IsFailure)
            return filled.Error;
        journal.Record(JournalAction.Created, userId, nowUtc, original.Number);
        return journal;
    }

    /// <summary>
    /// A journal the system writes and posts at once (a year-end close, or its reversal when the year reopens); the caller
    /// numbers it in the same transaction with <see cref="MarkPostedGenerated"/>.
    /// </summary>
    public static Result<JournalEntry> CreateGenerated(
        JournalType type, DateOnly postingDate, string description, string currencyCode, IReadOnlyList<JournalLineInput> lines,
        Guid? reversalOfJournalId, Guid? userId, DateTime nowUtc)
    {
        if (type is not (JournalType.YearEndClosing or JournalType.Reversal))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Only closing journals and their reversals are generated.");
        var journal = new JournalEntry
        {
            JournalType = type,
            Status = JournalStatus.Draft,
            CurrencyCode = currencyCode,
            LinesFixed = true,
            ReversalOfJournalId = reversalOfJournalId,
        };
        var filled = journal.Fill(postingDate, description, null, lines, MaxGeneratedLines);
        if (filled.IsFailure)
            return filled.Error;
        journal.Record(JournalAction.Created, userId, nowUtc, null);
        return journal;
    }

    /// <summary>
    /// A voucher draft: its lines are built from what the voucher says (cash account, party, amounts, tax, counter lines)
    /// and stay fixed; the voucher is changed only as a whole through <see cref="ReplaceVoucher"/>.
    /// </summary>
    public static Result<JournalEntry> CreateVoucher(VoucherInput input, Guid? userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!IsVoucherType(input.Type))
            throw new ArgumentOutOfRangeException(nameof(input), input.Type, "Not a voucher type.");
        var journal = new JournalEntry
        {
            JournalType = input.Type,
            Status = JournalStatus.Draft,
            CurrencyCode = input.CurrencyCode,
            RequiresApproval = input.RequiresApproval,
            LinesFixed = true,
        };
        var filled = journal.FillVoucher(input);
        if (filled.IsFailure)
            return filled.Error;
        journal.Record(JournalAction.Created, userId, nowUtc, null);
        return journal;
    }

    /// <summary>Replaces a voucher draft as a whole; its type stays.</summary>
    public Result ReplaceVoucher(VoucherInput input, Guid? userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (Status != JournalStatus.Draft)
            return JournalEntryDomainErrors.NotDraft;
        if (input.Type != JournalType)
            throw new InvalidOperationException($"Journal {Id} is a {JournalType}, not a {input.Type}.");
        var filled = FillVoucher(input);
        if (filled.IsFailure)
            return filled.Error;
        Record(JournalAction.Updated, userId, nowUtc, null);
        return Result.Success();
    }

    /// <summary>Replaces a draft's date, texts and (unless they are fixed) lines; null lines keep the current ones.</summary>
    public Result ReplaceDraft(
        DateOnly postingDate, string description, string? reference, IReadOnlyList<JournalLineInput>? lines, Guid? userId, DateTime nowUtc)
    {
        if (Status != JournalStatus.Draft)
            return JournalEntryDomainErrors.NotDraft;
        if (IsVoucher)
            return JournalEntryDomainErrors.VoucherManaged;
        if (LinesFixed && lines is not null)
            return JournalEntryDomainErrors.LinesFixed;
        var filled = Fill(postingDate, description, reference, lines ?? _lines.OrderBy(l => l.LineNumber).Select(l => l.ToInput()).ToList());
        if (filled.IsFailure)
            return filled.Error;
        Record(JournalAction.Updated, userId, nowUtc, null);
        return Result.Success();
    }

    /// <summary>Sends a draft that needs approval to the approvers.</summary>
    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != JournalStatus.Draft)
            return JournalEntryDomainErrors.NotDraft;
        if (!RequiresApproval)
            return JournalEntryDomainErrors.ApprovalNotRequired;
        Status = JournalStatus.Submitted;
        SubmittedByUserId = userId;
        Record(JournalAction.Submitted, userId, nowUtc, null);
        return Result.Success();
    }

    /// <summary>
    /// Whether it may post now: directly, when it is a draft that needs no approval; or by approval, when it is submitted
    /// (whoever wrote or submitted it may approve it).
    /// </summary>
    public Result CanPost(bool byApproval)
    {
        if (byApproval)
            return Status == JournalStatus.Submitted ? Result.Success() : JournalEntryDomainErrors.NotSubmitted;
        if (Status != JournalStatus.Draft)
            return JournalEntryDomainErrors.NotDraft;
        return RequiresApproval ? JournalEntryDomainErrors.ApprovalRequired : Result.Success();
    }

    /// <summary>Posts it under its number into the period the caller found open and locked for its date.</summary>
    public void MarkPosted(DocumentNumber number, Guid fiscalPeriodId, bool byApproval, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanPost(byApproval);
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Journal {Id} cannot be posted: {allowed.Error}");
        if (byApproval)
        {
            ApprovedByUserId = userId;
            Record(JournalAction.Approved, userId, nowUtc, null);
        }
        Post(number, fiscalPeriodId, userId, nowUtc);
    }

    /// <summary>Posts a generated journal: the system's own close carries the approval of whoever closed or reopened the year.</summary>
    public void MarkPostedGenerated(DocumentNumber number, Guid fiscalPeriodId, Guid? userId, DateTime nowUtc)
    {
        if (Status != JournalStatus.Draft || JournalType is not (JournalType.YearEndClosing or JournalType.Reversal) || !LinesFixed)
            throw new InvalidOperationException($"Journal {Id} is not a generated draft.");
        ApprovedByUserId = userId;
        Post(number, fiscalPeriodId, userId, nowUtc);
    }

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        var note = (reason ?? "").Trim();
        if (note.Length is 0 or > MaxNoteLength)
            return JournalEntryDomainErrors.ReasonRequired;
        if (Status != JournalStatus.Submitted)
            return JournalEntryDomainErrors.NotSubmitted;
        Status = JournalStatus.Draft;
        SubmittedByUserId = null;
        Record(JournalAction.Rejected, userId, nowUtc, note);
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (JournalStatus.Draft or JournalStatus.Submitted))
            return JournalEntryDomainErrors.NotOpen;
        Status = JournalStatus.Cancelled;
        Record(JournalAction.Cancelled, userId, nowUtc, null);
        return Result.Success();
    }

    /// <summary>Notes in its history that its reversal posted; the journal itself stays exactly as it was posted.</summary>
    public void RecordReversed(string reversalNumber, Guid? userId, DateTime nowUtc)
    {
        if (!IsPosted)
            throw new InvalidOperationException($"Journal {Id} is not posted, so nothing reversed it.");
        Record(JournalAction.Reversed, userId, nowUtc, reversalNumber);
    }

    /// <summary>Its lines turned the other way, in their order.</summary>
    public IReadOnlyList<JournalLineInput> Mirrored() =>
        _lines.OrderBy(l => l.LineNumber)
            .Select(l => new JournalLineInput(l.AccountId, l.BranchId, l.Credit, l.Debit, l.Description, l.PartyType, l.PartyId, l.DueDate))
            .ToList();

    private Result FillVoucher(VoucherInput input)
    {
        var lines = VoucherLines.Build(input);
        if (lines.IsFailure)
            return lines.Error;
        var filled = Fill(input.PostingDate, input.Description, input.Reference, lines.Value);
        if (filled.IsFailure)
            return filled.Error;
        CashAccountId = input.Cash?.CashAccountId;
        CounterCashAccountId = input.CounterCash?.CashAccountId;
        PartyType = input.Party?.Type;
        PartyId = input.Party?.PartyId;
        PaymentMethod = input.PaymentMethod;
        VoucherAmount = VoucherLines.Total(input);
        WithholdingAmount = input.WithholdingAmount == 0m ? null : input.WithholdingAmount;
        TaxAmount = input.TaxAmount == 0m ? null : input.TaxAmount;
        DueDate = input.Type == JournalType.ExpenseBill ? input.DueDate : null;
        _allocations.Clear();
        _allocations.AddRange(input.Allocations.Select(a => VoucherAllocation.Create(a.OpenItemId, a.Amount)));
        return Result.Success();
    }

    private void Post(DocumentNumber number, Guid fiscalPeriodId, Guid? userId, DateTime nowUtc)
    {
        Status = JournalStatus.Posted;
        Number = number.Number;
        FiscalYearId = number.FiscalYearId;
        SequenceNumber = number.SequenceNumber;
        FiscalPeriodId = fiscalPeriodId;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        Record(JournalAction.Posted, userId, nowUtc, null);
    }

    private Result Fill(
        DateOnly postingDate, string description, string? reference, IReadOnlyList<JournalLineInput> lines, int maxLines = MaxLines)
    {
        var text = (description ?? "").Trim();
        if (text.Length is 0 or > MaxDescriptionLength)
            return JournalEntryDomainErrors.DescriptionInvalid;
        var outside = Text.Optional(reference);
        if (outside?.Length > MaxReferenceLength)
            return JournalEntryDomainErrors.ReferenceTooLong;
        if (lines.Count < 2)
            return JournalEntryDomainErrors.TooFewLines;
        if (lines.Count > maxLines)
            return JournalEntryDomainErrors.TooManyLines;

        var decimals = Money.CurrencyDecimals(CurrencyCode);
        var built = new List<JournalLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var line = lines[i];
            if (line.AccountId == Guid.Empty || line.BranchId == Guid.Empty)
                return JournalEntryDomainErrors.LineIncomplete(number);
            if ((line.PartyType is null) != (line.PartyId is null) || line.PartyId == Guid.Empty
                || (line.DueDate is not null && line.PartyId is null))
                return JournalEntryDomainErrors.LinePartyIncomplete(number);
            if (line.Debit < 0 || line.Credit < 0 || (line.Debit > 0) == (line.Credit > 0))
                return JournalEntryDomainErrors.LineOneSide(number);
            var amount = line.Debit + line.Credit;
            if (!Money.FitsScale(amount, decimals))
                return JournalEntryDomainErrors.LineAmountScale(number, decimals);
            if (amount > Money.MaxLineAmount)
                return JournalEntryDomainErrors.LineAmountTooLarge(number);
            var lineText = Text.Optional(line.Description);
            if (lineText?.Length > JournalLine.MaxDescriptionLength)
                return JournalEntryDomainErrors.LineDescriptionTooLong(number);
            built.Add(JournalLine.Create(number, line, lineText));
        }
        var debit = built.Sum(l => l.Debit);
        var credit = built.Sum(l => l.Credit);
        if (debit != credit)
            return JournalEntryDomainErrors.Unbalanced(debit, credit);

        PostingDate = postingDate;
        Description = text;
        Reference = outside;
        _lines.Clear();
        _lines.AddRange(built);
        TotalDebit = debit;
        TotalCredit = credit;
        return Result.Success();
    }

    private void Record(JournalAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(JournalEvent.Create(action, userId, nowUtc, note));
}
