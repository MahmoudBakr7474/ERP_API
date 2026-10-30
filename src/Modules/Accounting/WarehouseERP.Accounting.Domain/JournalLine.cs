using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// One line of a journal: a posting account, the branch it belongs to, and a debit or a credit in the base currency. A line
/// on a control account names its customer or supplier and when it falls due; posting it opens the party's item.
/// </summary>
public sealed class JournalLine
{
    public const int MaxDescriptionLength = 250;

    private JournalLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid BranchId { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Description { get; private set; }
    public PartyType? PartyType { get; private set; }
    public Guid? PartyId { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public bool HasParty => PartyId is not null;

    internal static JournalLine Create(int lineNumber, JournalLineInput input, string? description) => new()
    {
        LineNumber = lineNumber,
        AccountId = input.AccountId,
        BranchId = input.BranchId,
        Debit = input.Debit,
        Credit = input.Credit,
        Description = description,
        PartyType = input.PartyType,
        PartyId = input.PartyId,
        DueDate = input.DueDate,
    };

    public JournalLineInput ToInput() => new(AccountId, BranchId, Debit, Credit, Description, PartyType, PartyId, DueDate);
}
