namespace WarehouseERP.Accounting.Contracts;

/// <summary>One step in a journal's history.</summary>
public enum JournalAction : byte
{
    /// <summary>The draft was written.</summary>
    Created = 1,

    /// <summary>The draft was changed.</summary>
    Updated = 2,

    /// <summary>Sent to the approvers.</summary>
    Submitted = 3,

    /// <summary>Accepted by an approver (posting follows in the same step).</summary>
    Approved = 4,

    /// <summary>Sent back to draft by an approver, with a reason.</summary>
    Rejected = 5,

    /// <summary>Entered the ledger under its number.</summary>
    Posted = 6,

    /// <summary>Dropped before posting.</summary>
    Cancelled = 7,

    /// <summary>Its reversal posted (the note names it); the journal itself stays in the ledger as it was.</summary>
    Reversed = 8,
}
