using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Imports.CommitCatalogImport;

/// <summary>
/// Applies a checked plan through the domain methods only (no SQL, no shortcut), in dependency order, without
/// saving: the caller's single save point commits it or nothing. A domain refusal here would mean the planner missed
/// a rule; it is returned as is and nothing is saved.
/// </summary>
public static class CatalogImportWriter
{
    private static readonly StringComparer Keys = StringComparer.OrdinalIgnoreCase;

    public static Result Apply(CatalogImportPlan plan, CatalogImportSnapshot current, ICatalogImportStore store)
    {
        var units = current.Units.ToDictionary(u => u.Code, Keys);
        foreach (var row in plan.Units)
        {
            if (row.Action == CatalogImportAction.Create)
            {
                var unit = Unit.Create(row.Code!, row.NameAr!, row.NameEn, row.AllowsFraction);
                store.Add(unit);
                units[unit.Code] = unit;
            }
            else if (row.Action == CatalogImportAction.Update)
                row.Existing!.Update(row.NameAr!, row.NameEn ?? row.Existing.NameEn, row.AllowsFraction);
        }
        var unitsById = units.Values.ToDictionary(u => u.Id);

        var categories = current.Categories.ToDictionary(c => c.Code, Keys);
        foreach (var row in plan.Categories.Where(c => c.Action == CatalogImportAction.Update))
            row.Existing!.Rename(row.NameAr!, row.NameEn ?? row.Existing.NameEn);
        // Parents before children; the planner refused loops and missing parents.
        var pending = plan.Categories.Where(c => c.Action == CatalogImportAction.Create).ToList();
        while (pending.Count > 0)
        {
            var ready = pending.Where(c => c.ParentCode is null || categories.ContainsKey(c.ParentCode)).ToList();
            if (ready.Count == 0)
                throw new InvalidOperationException("New categories refer to parents that are neither existing nor imported.");
            foreach (var row in ready)
            {
                var category = Category.Create(row.Code!, row.NameAr!, row.NameEn, row.ParentCode is { } parent ? categories[parent].Id : null);
                store.Add(category);
                categories[category.Code] = category;
                pending.Remove(row);
            }
        }

        var products = current.Products.ToDictionary(p => p.SKU, Keys);
        foreach (var row in plan.Products)
        {
            Guid? categoryId = row.CategoryCode is { } code ? categories[code].Id : null;
            if (row.Action == CatalogImportAction.Create)
            {
                var created = Product.Create(row.Sku!, row.NameAr!, row.NameEn, units[row.BaseUnitCode!].Id, categoryId, row.Type, row.Tracking);
                if (created.IsFailure)
                    return created.Error;
                store.Add(created.Value);
                products[created.Value.SKU] = created.Value;
            }
            else if (row.Action == CatalogImportAction.Update)
                row.Existing!.Update(row.NameAr!, row.NameEn ?? row.Existing.NameEn, categoryId ?? row.Existing.CategoryId);
        }

        foreach (var row in plan.ProductUnits.Where(u => u.Action == CatalogImportAction.Create))
        {
            var product = products[row.Sku!];
            var added = product.AddUnit(units[row.UnitCode!].Id, row.Factor, unitsById[product.BaseUnitId].AllowsFraction);
            if (added.IsFailure)
                return added.Error;
        }

        foreach (var row in plan.Barcodes.Where(b => b.Action == CatalogImportAction.Create))
        {
            var product = products[row.Sku!];
            var unitId = units[row.UnitCode!].Id;
            var added = product.AddBarcode(product.Units.Single(u => u.UnitId == unitId), row.Barcode!, row.Type);
            if (added.IsFailure)
                return added.Error;
        }
        return Result.Success();
    }
}
