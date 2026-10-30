namespace WarehouseERP.Accounting.Contracts;

/// <summary>One step in a fiscal year's history.</summary>
public enum FiscalYearAction : byte
{
    /// <summary>The year and its periods were created.</summary>
    Created = 1,

    /// <summary>One of its periods closed (the event names it).</summary>
    PeriodClosed = 2,

    /// <summary>One of its periods reopened, with a reason.</summary>
    PeriodReopened = 3,

    /// <summary>The year closed; the note names its closing journal, if any.</summary>
    Closed = 4,

    /// <summary>The year reopened, with a reason; its closing journal was reversed.</summary>
    Reopened = 5,
}
