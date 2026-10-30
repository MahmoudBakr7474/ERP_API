namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

/// <summary>A posted invoice (what it bills, with VAT) or an approved return's credit (negative) of a customer.</summary>
public sealed record CustomerReceivableDocument(Guid Id, decimal Amount);
