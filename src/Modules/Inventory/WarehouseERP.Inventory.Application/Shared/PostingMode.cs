namespace WarehouseERP.Inventory.Application.Shared;

/// <summary>How a document is posted.</summary>
public enum PostingMode
{
    /// <summary>A user posts a draft that needs no approval.</summary>
    Direct,

    /// <summary>An approver, who did not create it, approves a submitted document.</summary>
    Approval,

    /// <summary>A transfer or a count produced it; its approval rules were applied there.</summary>
    Generated,
}
