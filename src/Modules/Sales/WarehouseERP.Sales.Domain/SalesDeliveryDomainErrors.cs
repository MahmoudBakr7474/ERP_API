using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesDeliveryDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Sales.Delivery.NotDraft", "Only a draft delivery can be changed, posted or cancelled.");

    public static readonly Error NotPosted = Error.BusinessRule(
        "Sales.Delivery.NotPosted", "Only a posted delivery has goods to return.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Sales.Delivery.NoLines", "A delivery needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Sales.Delivery.TooManyLines", "A delivery holds at most 500 lines.");

    public static readonly Error DuplicatePick = Error.BusinessRule(
        "Sales.Delivery.DuplicatePick", "An order line is picked twice from the same location; add the quantities together.");

    public static Error LineNotFound(int line) => Error.BusinessRule(
        "Sales.Delivery.LineNotFound", $"Line {line}: the delivery has no such line.");

    public static Error ReturnExceedsDelivery(int line, decimal returnable) => Error.BusinessRule(
        "Sales.Delivery.ReturnExceedsDelivery",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {returnable} of this delivery line can still come back."));
}
