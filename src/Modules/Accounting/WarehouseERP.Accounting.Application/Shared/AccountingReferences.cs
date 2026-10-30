using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// What Accounting refers to through Organization's contracts: the company (its base currency is the ledger's currency)
// and its branches (every journal line names one).
public sealed class AccountingReferences(ICompanyDirectory companies, IBranchDirectory branches)
{
    public async Task<Result<CompanyInfo>> CompanyAsync(CancellationToken ct) =>
        await companies.GetAsync(ct) is { } company ? company : ReferenceErrors.CompanySetupRequired;

    /// <summary>The branches among these ids that exist, by id.</summary>
    public async Task<IReadOnlyDictionary<Guid, BranchInfo>> BranchesAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct) =>
        branchIds.Count == 0
            ? new Dictionary<Guid, BranchInfo>()
            : (await branches.FindManyAsync(branchIds, ct)).ToDictionary(b => b.Id);
}
