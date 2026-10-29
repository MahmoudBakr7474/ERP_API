namespace WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;

/// <param name="Search">Part of a customer's code, name, tax number or phone.</param>
/// <param name="Take">At most this many (default 10, at most 50).</param>
public sealed record SearchPosCustomersQuery(string Search, int? Take);
