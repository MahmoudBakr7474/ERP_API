using System.Text.RegularExpressions;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>
/// Decides, for every row of a batch against the current catalog, what confirming the import would do, and finds
/// every problem. Sheets are planned in dependency order (units, categories, products, product units, barcodes) so a
/// row may refer to a record created earlier in the same file. The rules are the single-record endpoints' rules
/// (ADR 0014); the import adds only what a file needs: duplicates within a sheet, references between sheets, and
/// fixed fields that an update must not try to change.
/// </summary>
public static partial class CatalogImportPlanner
{
    private static readonly StringComparer Keys = StringComparer.OrdinalIgnoreCase;

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex CodePattern();

    [GeneratedRegex("^[A-Za-z0-9._/-]+$")]
    private static partial Regex SkuPattern();

    /// <summary>The SKUs and barcode values a batch mentions: what <see cref="ICatalogImportStore.LoadAsync"/> must load.</summary>
    public static (IReadOnlyCollection<string> Skus, IReadOnlyCollection<string> Barcodes) References(CatalogImportBatch batch)
    {
        var skus = batch.Products.Select(r => r.Sku).Concat(batch.ProductUnits.Select(r => r.Sku)).Concat(batch.Barcodes.Select(r => r.Sku))
            .OfType<string>().Where(s => s.Length <= 80).Distinct(Keys).ToList();
        var barcodes = batch.Barcodes.Select(r => r.Barcode).OfType<string>().Where(b => b.Length <= 100).Distinct(Keys).ToList();
        return (skus, barcodes);
    }

    public static CatalogImportPlan Plan(
        CatalogImportBatch batch, CatalogImportMode mode, CatalogImportSnapshot current, CatalogImportPermissions can)
    {
        var plan = new CatalogImportPlan(mode);
        plan.Issues.AddRange(batch.FileIssues);
        if (batch.RowCount == 0 && batch.FileIssues.Count == 0)
            plan.Issues.Add(new CatalogImportIssue(CatalogImportSheet.Products, 1, null, ImportIssueCodes.NoRows,
                "The file has no rows to import. Fill in the template's sheets and keep their names and headers.", null));
        var context = new Context(plan, current, can);
        PlanUnits(context, batch.Units);
        PlanCategories(context, batch.Categories);
        PlanProducts(context, batch.Products);
        PlanProductUnits(context, batch.ProductUnits);
        PlanBarcodes(context, batch.Barcodes);
        return plan;
    }

    private static void PlanUnits(Context x, IReadOnlyList<UnitImportRow> rows)
    {
        var duplicates = DuplicateKeys(rows.Select(r => r.Code));
        foreach (var row in rows)
        {
            var check = x.Check(CatalogImportSheet.Units, row.Row);
            var code = check.Code("code", row.Code, 20, CodePattern());
            var nameAr = check.Required("nameAr", row.NameAr, 100);
            var nameEn = check.Optional("nameEn", row.NameEn, 100);
            var allows = check.Bool("allowsFraction", row.AllowsFraction);
            Duplicate(x, check, CatalogImportSheet.Units, "code", code, duplicates);

            var existing = code is null ? null : x.UnitsByCode.GetValueOrDefault(code);
            var changed = existing is not null && (existing.NameAr != nameAr || nameEn is not null && existing.NameEn != nameEn
                                                   || allows is { } a && existing.AllowsFraction != a);
            var action = Decide(x, check, existing is not null, changed, "code", UnitErrors.CodeTaken, x.Can.Units, code);
            var planned = new PlannedUnit(row.Row, code, nameAr, nameEn, allows ?? existing?.AllowsFraction ?? false, existing, action);
            x.Plan.Units.Add(planned);
            x.Plan.Rows.Add(new CatalogImportRowResult(CatalogImportSheet.Units, row.Row, code ?? row.Code, action));
            if (code is not null && !duplicates.Contains(code))
                x.PlannedUnits[code] = planned;
        }
    }

    private static void PlanCategories(Context x, IReadOnlyList<CategoryImportRow> rows)
    {
        var duplicates = DuplicateKeys(rows.Select(r => r.Code));
        var checks = new List<(PlannedCategory Planned, ImportRowCheck Check, bool Exists, bool Changed)>();
        foreach (var row in rows)
        {
            var check = x.Check(CatalogImportSheet.Categories, row.Row);
            var code = check.Code("code", row.Code, 30, CodePattern());
            var nameAr = check.Required("nameAr", row.NameAr, 150);
            var nameEn = check.Optional("nameEn", row.NameEn, 150);
            var parentCode = check.OptionalCode("parentCode", row.ParentCode, 30, CodePattern());
            Duplicate(x, check, CatalogImportSheet.Categories, "code", code, duplicates);

            var existing = code is null ? null : x.CategoriesByCode.GetValueOrDefault(code);
            if (existing is not null && x.Plan.Mode == CatalogImportMode.Update && !Keys.Equals(x.CategoryCode(existing.ParentId), parentCode))
                check.Fail("parentCode", ImportIssueCodes.FixedField,
                    "An import doesn't move categories. Keep the current parent here and move the category on the Categories screen.",
                    parentCode);
            var changed = existing is not null && (existing.NameAr != nameAr || nameEn is not null && existing.NameEn != nameEn);
            checks.Add((new PlannedCategory(row.Row, code, nameAr, nameEn, parentCode, existing, CatalogImportAction.Error), check,
                existing is not null, changed));
        }

        // New categories: a parent must exist and be active, or be another new category of this file; no loops. A
        // category whose parent row fails fails too, so repeat until nothing changes.
        var creating = checks.Where(c => !c.Exists && !c.Check.Failed && c.Planned.Code is { } code && !duplicates.Contains(code))
            .ToDictionary(c => c.Planned.Code!, Keys);
        bool more;
        do
        {
            more = false;
            foreach (var (planned, check, _, _) in creating.Values.Where(c => !c.Check.Failed).ToList())
            {
                if (ParentProblem(x, planned, creating, checks) is not { } problem)
                    continue;
                check.Fail("parentCode", problem.Code, problem.Message, planned.ParentCode);
                more = true;
            }
        } while (more);

        foreach (var (planned, check, exists, changed) in checks)
        {
            var action = Decide(x, check, exists, changed, "code", CategoryErrors.CodeTaken, x.Can.Categories, planned.Code);
            var final = planned with { Action = action };
            x.Plan.Categories.Add(final);
            x.Plan.Rows.Add(new CatalogImportRowResult(CatalogImportSheet.Categories, final.Row, final.Code, action));
            if (final.Code is not null && !duplicates.Contains(final.Code))
                x.PlannedCategories[final.Code] = final;
        }
    }

    private static (string Code, string Message)? ParentProblem(
        Context x, PlannedCategory planned, Dictionary<string, (PlannedCategory Planned, ImportRowCheck Check, bool Exists, bool Changed)> creating,
        List<(PlannedCategory Planned, ImportRowCheck Check, bool Exists, bool Changed)> all)
    {
        var seen = new HashSet<string>(Keys) { planned.Code! };
        var parentCode = planned.ParentCode;
        while (parentCode is not null)
        {
            if (!seen.Add(parentCode))
                return (CategoryErrors.Cycle.Code, CategoryErrors.Cycle.Message);
            if (creating.TryGetValue(parentCode, out var parent))
            {
                if (parent.Check.Failed)
                    return (ImportIssueCodes.ReferenceInvalid, $"The parent category {parentCode} has errors in this file.");
                parentCode = parent.Planned.ParentCode;
                continue;
            }
            if (all.Any(c => Keys.Equals(c.Planned.Code, parentCode) && c.Check.Failed))
                return (ImportIssueCodes.ReferenceInvalid, $"The parent category {parentCode} has errors in this file.");
            if (x.CategoriesByCode.GetValueOrDefault(parentCode) is not { } existing)
                return (CategoryErrors.NotFound.Code, $"No category has the code {parentCode}.");
            return existing.IsActive ? null : (CategoryErrors.ParentNotActive.Code, CategoryErrors.ParentNotActive.Message);
        }
        return null;
    }

    private static void PlanProducts(Context x, IReadOnlyList<ProductImportRow> rows)
    {
        var duplicates = DuplicateKeys(rows.Select(r => r.Sku));
        foreach (var row in rows)
        {
            var check = x.Check(CatalogImportSheet.Products, row.Row);
            var sku = check.Code("sku", row.Sku, 80, SkuPattern());
            var nameAr = check.Required("nameAr", row.NameAr, 250);
            var nameEn = check.Optional("nameEn", row.NameEn, 250);
            var baseUnitCode = check.Code("baseUnitCode", row.BaseUnitCode, 20, CodePattern());
            var categoryCode = check.OptionalCode("categoryCode", row.CategoryCode, 30, CodePattern());
            var type = check.Enum<ProductType>("type", row.Type, required: true);
            var tracking = check.Enum<TrackingMode>("trackingMode", row.TrackingMode, required: false) ?? TrackingMode.None;
            if (type is { } t && t != ProductType.StockItem && tracking != TrackingMode.None)
                check.Fail("trackingMode", ProductDomainErrors.TrackingNeedsStockItem.Code, ProductDomainErrors.TrackingNeedsStockItem.Message, row.TrackingMode);
            Duplicate(x, check, CatalogImportSheet.Products, "sku", sku, duplicates);

            var existing = sku is null ? null : x.ProductsBySku.GetValueOrDefault(sku);
            var changed = false;
            var baseAllows = false;
            if (existing is not null)
            {
                var currentBase = x.UnitCodes[existing.BaseUnitId];
                baseAllows = x.FindUnit(check, "baseUnitCode", currentBase, mustBeActive: false)?.AllowsFraction ?? false;
                var currentCategory = x.CategoryCode(existing.CategoryId);
                if (x.Plan.Mode == CatalogImportMode.Update)
                {
                    if (baseUnitCode is not null && !Keys.Equals(baseUnitCode, currentBase))
                        Fixed(check, "baseUnitCode", "the base unit", baseUnitCode);
                    if (type is { } newType && newType != existing.ProductType)
                        Fixed(check, "type", "the product type", row.Type);
                    if (tracking != existing.TrackingMode)
                        Fixed(check, "trackingMode", "the tracking mode", row.TrackingMode);
                    // A product may stay in a retired category, but can't be moved into one.
                    if (categoryCode is not null && !Keys.Equals(categoryCode, currentCategory))
                        x.FindCategory(check, "categoryCode", categoryCode, mustBeActive: true);
                }
                // Empty optional cells keep the current value: an import never clears data by omission.
                changed = existing.NameAr != nameAr || nameEn is not null && existing.NameEn != nameEn
                          || categoryCode is not null && !Keys.Equals(categoryCode, currentCategory);
            }
            else
            {
                if (baseUnitCode is not null)
                    baseAllows = x.FindUnit(check, "baseUnitCode", baseUnitCode, mustBeActive: true)?.AllowsFraction ?? false;
                if (categoryCode is not null)
                    x.FindCategory(check, "categoryCode", categoryCode, mustBeActive: true);
            }

            var action = Decide(x, check, existing is not null, changed, "sku", ProductErrors.SkuTaken,
                existing is null ? x.Can.CreateProducts : x.Can.UpdateProducts, sku);
            var planned = new PlannedProduct(row.Row, sku, nameAr, nameEn, baseUnitCode, categoryCode, type ?? ProductType.StockItem,
                tracking, baseAllows, existing, action);
            x.Plan.Products.Add(planned);
            x.Plan.Rows.Add(new CatalogImportRowResult(CatalogImportSheet.Products, row.Row, sku ?? row.Sku, action));
            if (sku is not null && !duplicates.Contains(sku))
                x.PlannedProducts[sku] = planned;
        }
    }

    private static void PlanProductUnits(Context x, IReadOnlyList<ProductUnitImportRow> rows)
    {
        var duplicates = DuplicateKeys(rows.Select(r => r.Sku is null || r.UnitCode is null ? null : $"{r.Sku}|{r.UnitCode}"));
        foreach (var row in rows)
        {
            var check = x.Check(CatalogImportSheet.ProductUnits, row.Row);
            var sku = check.Required("sku", row.Sku, 80);
            var unitCode = check.Code("unitCode", row.UnitCode, 20, CodePattern());
            var factor = check.Factor("conversionFactor", row.ConversionFactor);
            var key = sku is null || unitCode is null ? null : $"{sku}|{unitCode}";
            Duplicate(x, check, CatalogImportSheet.ProductUnits, "unitCode", key, duplicates);

            var product = sku is null ? null : x.FindProduct(check, sku);
            var exists = false;
            var baseRow = false;
            if (product is not null && unitCode is not null && factor is { } f)
            {
                if (Keys.Equals(unitCode, product.BaseUnitCode))
                {
                    // The base unit row always exists with factor 1; listing it is harmless.
                    baseRow = true;
                    if (f != 1m)
                        check.Fail("conversionFactor", ImportIssueCodes.BaseUnitFactor, "The base unit's factor is always 1.", row.ConversionFactor);
                }
                else if (product.Existing?.Units.FirstOrDefault(u => Keys.Equals(x.UnitCodes[u.UnitId], unitCode)) is { } current)
                {
                    exists = true;
                    if (x.Plan.Mode == CatalogImportMode.Update && current.ConversionFactor != f)
                        Fixed(check, "conversionFactor", "a unit's conversion factor", row.ConversionFactor);
                }
                else
                {
                    x.FindUnit(check, "unitCode", unitCode, mustBeActive: true);
                    if (product.Existing is { IsActive: false })
                        check.Fail("sku", ProductDomainErrors.NotActive.Code, ProductDomainErrors.NotActive.Message, sku);
                    if (!product.BaseAllowsFraction && f != decimal.Truncate(f))
                        check.Fail("conversionFactor", ProductUnitDomainErrors.FactorMustBeWhole.Code,
                            ProductUnitDomainErrors.FactorMustBeWhole.Message, row.ConversionFactor);
                }
            }

            var action = baseRow
                ? check.Failed ? CatalogImportAction.Error : CatalogImportAction.Unchanged
                : Decide(x, check, exists, changed: false, "unitCode", ProductUnitDomainErrors.UnitAlreadyAdded,
                    product?.Existing is null ? x.Can.CreateProducts : x.Can.UpdateProducts, unitCode);
            var planned = new PlannedProductUnit(row.Row, sku, unitCode, factor ?? 0m, action);
            x.Plan.ProductUnits.Add(planned);
            x.Plan.Rows.Add(new CatalogImportRowResult(CatalogImportSheet.ProductUnits, row.Row, key, action));
            if (key is not null && !duplicates.Contains(key))
                x.PlannedProductUnits[key] = planned;
        }
    }

    private static void PlanBarcodes(Context x, IReadOnlyList<BarcodeImportRow> rows)
    {
        var duplicates = DuplicateKeys(rows.Select(r => r.Barcode));
        foreach (var row in rows)
        {
            var check = x.Check(CatalogImportSheet.Barcodes, row.Row);
            var sku = check.Required("sku", row.Sku, 80);
            var unitCode = check.Code("unitCode", row.UnitCode, 20, CodePattern());
            var barcode = check.Required("barcode", row.Barcode, BarcodeFormat.MaxLength);
            var type = check.Enum<BarcodeType>("barcodeType", row.BarcodeType, required: true);
            if (barcode is not null && type is { } t && !BarcodeFormat.IsValid(t, barcode))
                check.Fail("barcode", ImportIssueCodes.BarcodeInvalid,
                    "EAN-13 has 13 digits and GTIN 8, 12, 13 or 14, ending in a valid GS1 check digit; other types are printable characters without spaces.",
                    barcode);
            Duplicate(x, check, CatalogImportSheet.Barcodes, "barcode", barcode, duplicates);

            var product = sku is null ? null : x.FindProduct(check, sku);
            var exists = false;
            if (product is not null && unitCode is not null && barcode is not null)
            {
                var unitActive = x.ProductUnitActive(check, product, sku!, unitCode);
                if (x.BarcodesByValue.GetValueOrDefault(barcode) is { } current)
                {
                    if (Keys.Equals(current.Sku, sku) && Keys.Equals(current.UnitCode, unitCode))
                    {
                        exists = true;
                        if (x.Plan.Mode == CatalogImportMode.Update && type is { } newType && newType != current.Type)
                            Fixed(check, "barcodeType", "a barcode's type", row.BarcodeType);
                    }
                    else
                        check.Fail("barcode", ProductBarcodeErrors.Taken.Code,
                            $"This barcode is already used by {current.Sku} ({current.UnitCode}); barcode values are never reused.", barcode);
                }
                else
                {
                    if (product.Existing is { IsActive: false })
                        check.Fail("sku", ProductDomainErrors.NotActive.Code, ProductDomainErrors.NotActive.Message, sku);
                    if (unitActive is false)
                        check.Fail("unitCode", ProductUnitDomainErrors.NotActive.Code, ProductUnitDomainErrors.NotActive.Message, unitCode);
                }
            }

            var action = Decide(x, check, exists, changed: false, "barcode", ProductBarcodeErrors.Taken,
                product?.Existing is null ? x.Can.CreateProducts : x.Can.UpdateProducts, barcode);
            x.Plan.Barcodes.Add(new PlannedBarcode(row.Row, sku, unitCode, barcode, type ?? BarcodeType.Internal, action));
            x.Plan.Rows.Add(new CatalogImportRowResult(CatalogImportSheet.Barcodes, row.Row, barcode ?? row.Barcode, action));
        }
    }

    // The row's outcome. A key that exists is an error in Create mode, left alone in Skip mode and updated (or found
    // unchanged) in Update mode. Creating or changing needs the permission; leaving things alone doesn't.
    private static CatalogImportAction Decide(
        Context x, ImportRowCheck check, bool exists, bool changed, string keyField, Error taken, bool permitted, string? key)
    {
        if (exists && x.Plan.Mode == CatalogImportMode.Create)
            check.Fail(keyField, taken.Code, $"{taken.Message} Create mode only adds new records.", key);
        if (check.Failed)
            return CatalogImportAction.Error;
        var action = !exists ? CatalogImportAction.Create
            : x.Plan.Mode == CatalogImportMode.Skip ? CatalogImportAction.Skip
            : changed ? CatalogImportAction.Update
            : CatalogImportAction.Unchanged;
        if (action is CatalogImportAction.Create or CatalogImportAction.Update && !permitted)
        {
            check.Fail(null, ImportIssueCodes.NotPermitted, "You don't have the permission this row needs.");
            return CatalogImportAction.Error;
        }
        return action;
    }

    private static void Fixed(ImportRowCheck check, string field, string what, string? value) =>
        check.Fail(field, ImportIssueCodes.FixedField, $"An import can't change {what} of an existing record; it is fixed once created.", value);

    private static void Duplicate(Context x, ImportRowCheck check, CatalogImportSheet sheet, string field, string? key, HashSet<string> duplicates)
    {
        if (key is null || !duplicates.Contains(key))
            return;
        check.Fail(field, ImportIssueCodes.DuplicateInFile, "The same key appears more than once in this sheet.", key);
        x.Plan.Duplicates.Add((sheet, check.Row));
    }

    private static HashSet<string> DuplicateKeys(IEnumerable<string?> keys) =>
        keys.OfType<string>().GroupBy(k => k, Keys).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(Keys);

    /// <summary>What later sheets know about a product: an existing one, or a new one of this file.</summary>
    private sealed record ProductRef(Product? Existing, string BaseUnitCode, bool BaseAllowsFraction);

    private sealed record UnitRef(bool IsActive, bool AllowsFraction);

    private sealed class Context(CatalogImportPlan plan, CatalogImportSnapshot current, CatalogImportPermissions can)
    {
        public CatalogImportPlan Plan => plan;
        public CatalogImportPermissions Can => can;
        public Dictionary<string, Unit> UnitsByCode { get; } = current.Units.ToDictionary(u => u.Code, Keys);
        public Dictionary<Guid, string> UnitCodes { get; } = current.Units.ToDictionary(u => u.Id, u => u.Code);
        public Dictionary<string, Category> CategoriesByCode { get; } = current.Categories.ToDictionary(c => c.Code, Keys);
        public Dictionary<Guid, string> CategoryCodes { get; } = current.Categories.ToDictionary(c => c.Id, c => c.Code);
        public Dictionary<string, Product> ProductsBySku { get; } = current.Products.ToDictionary(p => p.SKU, Keys);
        public Dictionary<string, ExistingBarcode> BarcodesByValue { get; } = current.Barcodes.ToDictionary(b => b.Barcode, Keys);
        public Dictionary<string, PlannedUnit> PlannedUnits { get; } = new(Keys);
        public Dictionary<string, PlannedCategory> PlannedCategories { get; } = new(Keys);
        public Dictionary<string, PlannedProduct> PlannedProducts { get; } = new(Keys);
        public Dictionary<string, PlannedProductUnit> PlannedProductUnits { get; } = new(Keys);

        public ImportRowCheck Check(CatalogImportSheet sheet, int row) => new(plan.Issues, sheet, row);

        public string? CategoryCode(Guid? id) => id is { } value ? CategoryCodes.GetValueOrDefault(value) : null;

        /// <summary>A unit by code as it will be after this import (a Units row changes its fraction setting first).</summary>
        public UnitRef? FindUnit(ImportRowCheck check, string field, string code, bool mustBeActive)
        {
            UnitRef? found = null;
            if (PlannedUnits.TryGetValue(code, out var planned))
            {
                if (planned.Action == CatalogImportAction.Error)
                    check.Fail(field, ImportIssueCodes.ReferenceInvalid, $"The unit {code} has errors in the Units sheet.", code);
                else
                    found = new UnitRef(planned.Existing?.IsActive ?? true,
                        planned.Action == CatalogImportAction.Skip ? planned.Existing!.AllowsFraction : planned.AllowsFraction);
            }
            else if (UnitsByCode.TryGetValue(code, out var unit))
                found = new UnitRef(unit.IsActive, unit.AllowsFraction);
            else
                check.Fail(field, UnitErrors.NotFound.Code, $"No unit has the code {code}.", code);

            if (found is { IsActive: false } && mustBeActive)
                check.Fail(field, UnitErrors.NotActive.Code, UnitErrors.NotActive.Message, code);
            return found;
        }

        public void FindCategory(ImportRowCheck check, string field, string code, bool mustBeActive)
        {
            bool? active = null;
            if (PlannedCategories.TryGetValue(code, out var planned))
            {
                if (planned.Action == CatalogImportAction.Error)
                    check.Fail(field, ImportIssueCodes.ReferenceInvalid, $"The category {code} has errors in the Categories sheet.", code);
                else
                    active = planned.Existing?.IsActive ?? true;
            }
            else if (CategoriesByCode.TryGetValue(code, out var category))
                active = category.IsActive;
            else
                check.Fail(field, CategoryErrors.NotFound.Code, $"No category has the code {code}.", code);

            if (active is false && mustBeActive)
                check.Fail(field, CategoryErrors.NotActive.Code, CategoryErrors.NotActive.Message, code);
        }

        public ProductRef? FindProduct(ImportRowCheck check, string sku)
        {
            if (PlannedProducts.TryGetValue(sku, out var planned))
            {
                if (planned.Action == CatalogImportAction.Error)
                {
                    check.Fail("sku", ImportIssueCodes.ReferenceInvalid, $"The product {sku} has errors in the Products sheet.", sku);
                    return null;
                }
                if (planned.Existing is null)
                    return new ProductRef(null, planned.BaseUnitCode!, planned.BaseAllowsFraction);
            }
            if (ProductsBySku.TryGetValue(sku, out var product))
            {
                var baseCode = UnitCodes[product.BaseUnitId];
                var allows = PlannedUnits.TryGetValue(baseCode, out var unit) && unit.Action is not (CatalogImportAction.Error or CatalogImportAction.Skip)
                    ? unit.AllowsFraction
                    : UnitsByCode[baseCode].AllowsFraction;
                return new ProductRef(product, baseCode, allows);
            }
            check.Fail("sku", ProductErrors.NotFound.Code, $"No product has the SKU {sku}, here or in the Products sheet.", sku);
            return null;
        }

        /// <summary>Whether the product's unit is active, or null (with an error) when the product has no such unit.</summary>
        public bool? ProductUnitActive(ImportRowCheck check, ProductRef product, string sku, string unitCode)
        {
            if (Keys.Equals(unitCode, product.BaseUnitCode))
                return true; // the base unit always stays active
            if (product.Existing?.Units.FirstOrDefault(u => Keys.Equals(UnitCodes[u.UnitId], unitCode)) is { } current)
                return current.IsActive;
            if (PlannedProductUnits.TryGetValue($"{sku}|{unitCode}", out var planned))
            {
                if (planned.Action != CatalogImportAction.Error)
                    return true;
                check.Fail("unitCode", ImportIssueCodes.ReferenceInvalid, $"The unit {unitCode} of {sku} has errors in the ProductUnits sheet.", unitCode);
                return null;
            }
            check.Fail("unitCode", ProductUnitErrors.NotFound.Code,
                $"{sku} has no unit {unitCode}. Add it in the ProductUnits sheet first.", unitCode);
            return null;
        }
    }
}
