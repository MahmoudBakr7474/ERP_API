using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts;

// Builds a whole chart from rows, all or nothing: codes unique, every parent named by code and present, each account
// passing the same domain rules as one created by hand. Parents are built first (a child's code is longer than its
// parent's), and each account is added at once so the context gives it the id its children point at.
internal static class ChartBuilder
{
    public const int MaxRows = 5000;

    /// <summary>The created accounts by code, or the first row refused.</summary>
    public static Result<IReadOnlyDictionary<string, Account>> Build(IReadOnlyList<ChartAccountRow> rows, Action<Account> add)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var code = (row.Code ?? "").Trim();
            if (!codes.Add(code))
                return AccountErrors.ImportCodeDuplicate(code);
        }

        var created = new Dictionary<string, Account>(StringComparer.Ordinal);
        foreach (var row in rows.OrderBy(r => (r.Code ?? "").Trim().Length).ThenBy(r => (r.Code ?? "").Trim(), StringComparer.Ordinal))
        {
            var code = (row.Code ?? "").Trim();
            Account? parent = null;
            var parentCode = Text.Optional(row.ParentCode);
            if (parentCode is not null && !created.TryGetValue(parentCode, out parent))
                return AccountErrors.ImportParentMissing(code, parentCode);
            var account = Account.Create(code, row.NameAr, row.NameEn, parent, row.Type, row.Section, row.IsPosting,
                row.AllowsManualPosting, row.PartyType);
            if (account.IsFailure)
                return AccountErrors.ImportRowRefused(code, account.Error);
            add(account.Value);
            created.Add(code, account.Value);
        }
        return Result.Success<IReadOnlyDictionary<string, Account>>(created);
    }

    /// <summary>Maps each role to the built account of its code, by the same rules as a mapping made by hand; the count mapped.</summary>
    public static Result<int> MapRoles(
        IReadOnlyList<ChartRoleRow> roles, IReadOnlyDictionary<string, Account> accounts, Action<AccountMapping> add)
    {
        foreach (var row in roles.OrderBy(r => r.Role))
        {
            if (!accounts.TryGetValue((row.AccountCode ?? "").Trim(), out var account))
                return AccountErrors.ImportRoleAccountMissing(row.Role.ToString(), row.AccountCode ?? "");
            var mapping = AccountMapping.Create(row.Role, account);
            if (mapping.IsFailure)
                return mapping.Error;
            add(mapping.Value);
        }
        return roles.Count;
    }
}
