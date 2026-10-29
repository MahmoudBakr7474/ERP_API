using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Sales;

/// <summary>A cart as Sales priced it, in the base currency, with the discount limit it was checked against.</summary>
public sealed record PricedCart(string CurrencyCode, int CurrencyDecimals, IReadOnlyList<PosSaleLineInput> Lines, decimal MaxLineDiscountPercent);
