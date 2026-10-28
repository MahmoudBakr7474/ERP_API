using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reconciliation;
using WarehouseERP.Inventory.Application.Contracts.Reconciliation;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Reconciliation.GetReconciliation;

// Reconciliation spans every warehouse and the company-wide valuation, so it needs company-wide access. It only reports.
public sealed class GetReconciliationHandler(IReconciliationReadStore reconciliation, ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetReconciliationQuery, ReconciliationReport>
{
    public async Task<Result<ReconciliationReport>> HandleAsync(GetReconciliationQuery query, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return ReconciliationErrors.CompanyWideAccessRequired;
        return await reconciliation.GetAsync(clock.GetUtcNow().UtcDateTime, ct);
    }
}
