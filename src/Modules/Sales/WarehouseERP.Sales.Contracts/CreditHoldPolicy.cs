namespace WarehouseERP.Sales.Contracts;

/// <summary>What a customer's manual credit hold does to a new order.</summary>
public enum CreditHoldPolicy : byte
{
    /// <summary>The order can't be confirmed while the customer is on hold.</summary>
    BlockConfirmation = 1,

    /// <summary>The order needs approval while the customer is on hold.</summary>
    RequireApproval = 2,
}
