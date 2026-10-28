using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

public static class DocumentPolicyErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.DocumentPolicy.NotFound", "Only purchase requests and purchase orders have an approval policy.");
}
