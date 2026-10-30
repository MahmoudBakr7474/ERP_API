namespace WarehouseERP.Accounting.Application.Contracts.Accounts;

/// <summary>What an import or the default chart created.</summary>
public sealed record ChartImportedResponse(int AccountsCreated, int RolesMapped);
