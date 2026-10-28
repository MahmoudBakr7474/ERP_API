using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.UpdateSupplierReturn;

// Only a draft changes, only from the version its editor read (409 otherwise). Returns the new row version.
public sealed partial class UpdateSupplierReturnHandler(
    ISupplierReturnRepository returns, IGoodsReceiptRepository receipts, ICurrentUser currentUser, ILogger<UpdateSupplierReturnHandler> logger)
    : ICommandHandler<UpdateSupplierReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSupplierReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var supplierReturn = await returns.GetForUpdateAsync(command.SupplierReturnId, expected, ct);
        if (supplierReturn is null)
            return SupplierReturnErrors.NotFound;
        var receipt = await receipts.GetForUpdateAsync(supplierReturn.GoodsReceiptId, ct)
            ?? throw new InvalidOperationException($"Return {supplierReturn.Id} points at receipt {supplierReturn.GoodsReceiptId}, which does not exist.");

        var replaced = supplierReturn.ReplaceDraft(receipt, command.Reason, command.Notes, ReturnLines.Inputs(command.Lines));
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnUpdated(logger, supplierReturn.Id);
        return Convert.ToBase64String(supplierReturn.RowVersion);
    }

    [LoggerMessage(EventId = 6041, Level = LogLevel.Information, Message = "Supplier return {ReturnId} draft updated")]
    private static partial void ReturnUpdated(ILogger logger, Guid returnId);
}
