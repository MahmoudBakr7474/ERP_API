using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.CreateSalesQuotation;

// A draft quotation to an active customer from one selling warehouse in the user's scope, in the company's base currency,
// its lines priced from the price list (or by a user allowed to set prices). It reserves nothing.
public sealed partial class CreateSalesQuotationHandler(
    ISalesQuotationRepository quotations, ISalesSettingsRepository settings, SalesReferences references, LinePricing pricing,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateSalesQuotationHandler> logger)
    : ICommandHandler<CreateSalesQuotationCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSalesQuotationCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return AccessErrors.WarehouseDenied;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var warehouse = await references.SellingWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var customer = await references.ActiveCustomerAsync(command.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var lines = await pricing.PriceAsync(command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var date = command.QuotationDate ?? references.Today(company.Value);
        var validUntil = command.ValidUntil ?? date.AddDays((await settings.GetAsync(ct)).QuotationValidityDays);
        var terms = new QuotationTerms(command.CustomerId, date, validUntil, command.CustomerReference,
            command.PaymentTermDays ?? customer.Value.PaymentTermDays, command.Notes);
        var created = SalesQuotation.CreateDraft(command.WarehouseId, warehouse.Value.BranchId, company.Value.BaseCurrencyCode, terms,
            lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        quotations.Add(created.Value);
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationCreated(logger, created.Value.Id, command.CustomerId);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7020, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} drafted for customer {CustomerId}")]
    private static partial void QuotationCreated(ILogger logger, Guid quotationId, Guid customerId);
}
