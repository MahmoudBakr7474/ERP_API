using WarehouseERP.Accounting.Application.Contracts.Accounts;

namespace WarehouseERP.Accounting.Application.Features.Reports;

// The chart as reports walk it: accounts by id, each header's posting accounts below it, and each account's ancestors.
internal sealed class ReportAccounts
{
    private readonly Dictionary<Guid, AccountSummaryResponse> _byId;
    private readonly ILookup<Guid?, AccountSummaryResponse> _children;

    public ReportAccounts(IReadOnlyList<AccountSummaryResponse> accounts)
    {
        All = accounts;
        _byId = accounts.ToDictionary(a => a.Id);
        _children = accounts.ToLookup(a => a.ParentId);
    }

    /// <summary>Every account in code order.</summary>
    public IReadOnlyList<AccountSummaryResponse> All { get; }

    public AccountSummaryResponse? Find(Guid accountId) => _byId.GetValueOrDefault(accountId);

    /// <summary>The account itself when it posts, otherwise every posting account below it.</summary>
    public IReadOnlyList<Guid> PostingAccountsUnder(AccountSummaryResponse account)
    {
        var found = new List<Guid>();
        var pending = new Stack<AccountSummaryResponse>([account]);
        while (pending.TryPop(out var next))
        {
            if (next.IsPosting)
                found.Add(next.Id);
            foreach (var child in _children[next.Id])
                pending.Push(child);
        }
        return found;
    }

    /// <summary>The account's parent, grandparent and so on up to its class.</summary>
    public IEnumerable<AccountSummaryResponse> Ancestors(AccountSummaryResponse account)
    {
        var parentId = account.ParentId;
        while (parentId is { } id && _byId.TryGetValue(id, out var parent))
        {
            yield return parent;
            parentId = parent.ParentId;
        }
    }
}
