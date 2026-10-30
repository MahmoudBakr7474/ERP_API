using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Presentation.Imports;

internal sealed record ImportSheetLayout(CatalogImportSheet Sheet, string NameEn, string NameAr, IReadOnlyList<ImportColumn> Columns)
{
    public string Name(bool arabic) => arabic ? NameAr : NameEn;
}
