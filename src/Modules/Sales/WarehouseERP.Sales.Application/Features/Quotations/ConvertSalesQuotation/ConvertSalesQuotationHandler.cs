using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.ConvertSalesQuotation;

// One transaction: the issued quotation (still valid today, its customer still active and its products still sold)
// becomes a draft sales order of the same customer and warehouse with the same lines, prices, discounts and taxes (the
// quotation's snapshot, never re-priced), its payment terms and reference; then it is marked converted, so it can't
// become a second order. The order is confirmed like any other: approval rules and reservations apply then. Returns the
// new order's id.
public sealed partial class ConvertSalesQuotationHandler(
    ITransactionRunner transactions, ISalesQuotationRepository quotations, ISalesOrderRepository orders, SalesReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ConvertSalesQuotationHandler> logger)
    : ICommandHandler<ConvertSalesQuotationCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ConvertSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ConvertAsync(command.QuotationId, expected, token), ct);
    }

    private async Task<Result<Guid>> ConvertAsync(Guid quotationId, byte[] expected, CancellationToken ct)
    {
        var quotation = await quotations.GetForUpdateAsync(quotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var today = references.Today(company.Value);
        var convertible = quotation.CanConvert(today);
        if (convertible.IsFailure)
            return convertible.Error;
        var customer = await references.ActiveCustomerAsync(quotation.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var warehouse = await references.SellingWarehouseAsync(quotation.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var lines = quotation.Lines.OrderBy(l => l.LineNumber).ToList();
        var units = await references.ResolveUnitsAsync(lines.Select(l => new ProductUnitRef(l.ProductId, l.ProductUnitId)).ToList(), ct);
        if (units.IsFailure)
            return units.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var terms = new OrderTerms(quotation.CustomerId, today, RequestedDeliveryDate: null, quotation.CustomerReference,
            quotation.PaymentTermDays, ShippingAddress: null, quotation.Notes);
        var created = SalesOrder.CreateDraft(quotation.WarehouseId, quotation.BranchId, quotation.CurrencyCode, terms,
            lines.Select((l, i) => l.ToInput(units.Value[i])).ToList(), quotation.Id, currentUser.UserId, now);
        if (created.IsFailure)
            return created.Error;
        orders.Add(created.Value);
        var converted = quotation.MarkConverted(created.Value.Id, today, currentUser.UserId, now);
        if (converted.IsFailure)
            return converted.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationConverted(logger, quotation.Id, created.Value.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7027, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} converted into sales order {OrderId}")]
    private static partial void QuotationConverted(ILogger logger, Guid quotationId, Guid orderId);
}
