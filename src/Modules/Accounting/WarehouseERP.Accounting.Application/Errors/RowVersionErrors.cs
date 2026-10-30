using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class RowVersionErrors
{
    /// <summary>What a handler returns when a direct caller skipped the validator and sent a bad row version.</summary>
    public static readonly Error Invalid = Error.Validation(
        [new ValidationError("RowVersion", "RowVersionValidator", "Invalid row version.")]);
}
