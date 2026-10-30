using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Shared;

/// <summary>A customer (from Sales) or supplier (from Purchasing) as Accounting names it.</summary>
/// <param name="PaymentTermDays">A supplier's payment terms; zero for a customer (their terms live on Sales' orders).</param>
public sealed record PartyInfo(PartyType Type, Guid Id, string Code, string NameAr, string? NameEn, bool IsActive, int PaymentTermDays);
