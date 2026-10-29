using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales.ReprintPosSale;

// A copy of a receipt (pos.receipts.reprint, checked by the endpoint): counted on the sale with who printed it last, and
// returned marked as a copy with its number.
public sealed partial class ReprintPosSaleHandler(
    ITransactionRunner transactions, IPosSaleRepository sales, IPosSaleReadStore reads, ICurrentUser currentUser, TimeProvider clock,
    ILogger<ReprintPosSaleHandler> logger)
    : ICommandHandler<ReprintPosSaleCommand, PosSaleResponse>
{
    public async Task<Result<PosSaleResponse>> HandleAsync(ReprintPosSaleCommand command, CancellationToken ct)
    {
        var access = PosAccess.Check(currentUser, await sales.FindWarehouseAsync(command.SaleId, ct), SaleErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync<PosSaleResponse>(async token =>
        {
            var sale = await sales.GetForUpdateAsync(command.SaleId, token);
            if (sale is null)
                return SaleErrors.NotFound;
            var printed = sale.RecordReprint(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
            var saved = await sales.SaveChangesAsync(token);
            if (saved.IsFailure)
                return saved.Error;
            ReceiptReprinted(logger, sale.Id, sale.Number, printed - 1);
            var receipt = await reads.GetAsync(sale.Id, token)
                ?? throw new InvalidOperationException($"Sale {sale.Id} can't be read back.");
            return receipt with { IsCopy = true, CopyNumber = printed - 1 };
        }, ct);
    }

    [LoggerMessage(EventId = 8033, Level = LogLevel.Information, Message = "POS sale {SaleId} ({Number}): copy {CopyNumber} printed")]
    private static partial void ReceiptReprinted(ILogger logger, Guid saleId, string number, int copyNumber);
}
