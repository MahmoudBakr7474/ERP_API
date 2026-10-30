using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// The posting account a role uses when the system writes a journal (year-end close, ERP postings). One row per mapped
/// role; versioned in a temporal table, so every earlier mapping stays visible.
/// </summary>
public sealed class AccountMapping : IAuditable
{
    private AccountMapping() { } // EF Core

    public PostingRole Role { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Result<AccountMapping> Create(PostingRole role, Account account)
    {
        var fits = PostingRoleRules.Check(role, account);
        return fits.IsFailure ? fits.Error : new AccountMapping { Role = role, AccountId = account.Id };
    }

    public Result Remap(Account account)
    {
        var fits = PostingRoleRules.Check(Role, account);
        if (fits.IsFailure)
            return fits.Error;
        AccountId = account.Id;
        return Result.Success();
    }
}
