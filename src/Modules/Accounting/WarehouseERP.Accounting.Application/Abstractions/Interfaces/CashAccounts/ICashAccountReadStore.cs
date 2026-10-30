using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;

/// <summary>Read side of cashboxes and bank accounts, with their posted balances.</summary>
public interface ICashAccountReadStore
{
    Task<CashAccountResponse?> GetAsync(Guid cashAccountId, CancellationToken ct);

    /// <summary>Every cash account (a company has few) in code order, balances as of the end of a date.</summary>
    Task<IReadOnlyList<CashAccountResponse>> ListAsync(CashAccountKind? kind, Guid? branchId, bool? isActive, DateOnly? asOfDate, CancellationToken ct);
}
