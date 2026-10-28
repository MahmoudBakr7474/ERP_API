using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.GetGoodsReceipt;

public sealed class GetGoodsReceiptHandler(IGoodsReceiptRepository receipts, IGoodsReceiptReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetGoodsReceiptQuery, GoodsReceiptDetailsResponse>
{
    public async Task<Result<GoodsReceiptDetailsResponse>> HandleAsync(GetGoodsReceiptQuery query, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await receipts.FindWarehouseAsync(query.GoodsReceiptId, ct), GoodsReceiptErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.GoodsReceiptId, ct) is { } receipt ? receipt : GoodsReceiptErrors.NotFound;
    }
}
