using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>Which accounts a posting role may be mapped to: an active posting account of a fitting type and party.</summary>
public static class PostingRoleRules
{
    public static IReadOnlyList<AccountType> TypesOf(PostingRole role) => role switch
    {
        PostingRole.RetainedEarnings or PostingRole.OpeningBalanceEquity => [AccountType.Equity],
        PostingRole.Inventory or PostingRole.AccountsReceivable or PostingRole.UnbilledReceivables or PostingRole.InputVat
            or PostingRole.WithholdingTaxReceivable or PostingRole.CardClearing or PostingRole.PosTillClearing => [AccountType.Asset],
        PostingRole.GoodsReceivedNotInvoiced or PostingRole.AccountsPayable or PostingRole.OutputVat
            or PostingRole.WithholdingTaxPayable or PostingRole.UnmatchedReceipts => [AccountType.Liability],
        PostingRole.PurchaseVarianceUnderReview => [AccountType.Asset, AccountType.Liability],
        PostingRole.SalesRevenue or PostingRole.SalesReturns or PostingRole.PosRevenue or PostingRole.PosReturns => [AccountType.Revenue],
        PostingRole.InventoryGains => [AccountType.Revenue, AccountType.Expense],
        PostingRole.PurchasePriceVariance or PostingRole.CostOfSales or PostingRole.InventoryLosses or PostingRole.InventoryAdjustments
            or PostingRole.StockCountDifferences or PostingRole.InternalIssues or PostingRole.CashOverShort or PostingRole.CardFees
            => [AccountType.Expense],
        _ => [],
    };

    /// <summary>The party a role's account names on every line: customers for receivables, suppliers for payables.</summary>
    public static PartyType? PartyOf(PostingRole role) => role switch
    {
        PostingRole.AccountsReceivable => PartyType.Customer,
        PostingRole.AccountsPayable => PartyType.Supplier,
        _ => null,
    };

    public static Result Check(PostingRole role, Account account)
    {
        if (!Enum.IsDefined(role))
            return AccountMappingDomainErrors.RoleInvalid;
        if (!account.IsPosting)
            return AccountMappingDomainErrors.NotPostingAccount;
        if (!account.IsActive)
            return AccountMappingDomainErrors.AccountNotActive;
        if (!TypesOf(role).Contains(account.Type) || account.PartyType != PartyOf(role))
            return AccountMappingDomainErrors.AccountDoesNotFitRole(role);
        return Result.Success();
    }
}
