using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ListTransferDestinations;

// The warehouse list of Organization shows a user's own scope; a storekeeper of the source warehouse must still be able
// to name a destination outside it. Only names: receiving there still needs the destination in scope.
public sealed class ListTransferDestinationsHandler(IWarehouseListing warehouses)
    : IQueryHandler<ListTransferDestinationsQuery, IReadOnlyList<TransferDestinationResponse>>
{
    public async Task<Result<IReadOnlyList<TransferDestinationResponse>>> HandleAsync(ListTransferDestinationsQuery query, CancellationToken ct)
    {
        var listed = await warehouses.ListActiveAsync(WarehouseType.Physical, ct);
        return listed.Select(w => new TransferDestinationResponse(w.Id, w.Code, w.NameAr, w.NameEn, w.BranchId, w.BranchCode)).ToList();
    }
}
