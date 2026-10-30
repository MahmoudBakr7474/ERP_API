using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

// The organization reports, read straight from the Organization tables (no stored figures). A scoped caller sees their
// warehouses, those warehouses' locations and the branches holding them; company rows are visible to everyone who may
// read the reports, as the company itself is.
internal sealed class OrganizationReportReadStore(DbContext db) : IOrganizationReportReadStore
{
    public async Task<OrganizationSummaryResponse> GetSummaryAsync(IReadOnlySet<Guid>? scope, CancellationToken ct)
    {
        var warehouses = Warehouses(scope);
        var branches = Branches(scope);
        var storage = Locations(scope).Where(l => l.Warehouse.WarehouseType == WarehouseType.Physical);
        var issues = await ListDataIssuesAsync(scope, ct);
        return new OrganizationSummaryResponse(
            await branches.CountAsync(ct),
            await branches.CountAsync(b => b.IsActive, ct),
            await warehouses.CountAsync(ct),
            await warehouses.CountAsync(w => w.IsActive, ct),
            await warehouses.CountAsync(w => w.WarehouseType == WarehouseType.Physical, ct),
            await warehouses.CountAsync(w => w.WarehouseType == WarehouseType.Transit, ct),
            await warehouses.CountAsync(w => w.IsActive && w.AllowsSelling, ct),
            await storage.CountAsync(ct),
            await storage.CountAsync(l => l.IsActive, ct),
            // A transit warehouse's default location is not a transfer; each dispatched transfer opens its own location.
            await Locations(scope).CountAsync(l => l.IsActive && !l.IsDefault && l.Warehouse.WarehouseType == WarehouseType.Transit, ct),
            issues.Count);
    }

    public async Task<IReadOnlyList<StructureBranchResponse>> GetStructureAsync(
        bool? isActive, IReadOnlySet<Guid>? scope, CancellationToken ct)
    {
        var branchQuery = Branches(scope);
        var warehouseQuery = Warehouses(scope);
        if (isActive is { } active)
        {
            branchQuery = branchQuery.Where(b => b.IsActive == active);
            warehouseQuery = warehouseQuery.Where(w => w.IsActive == active);
        }

        var branches = await branchQuery.OrderBy(b => b.Code)
            .Select(b => new { b.Id, b.Code, b.NameAr, b.NameEn, b.IsActive })
            .ToListAsync(ct);
        var warehouses = await warehouseQuery.OrderBy(w => w.Code)
            .Select(w => new
            {
                w.BranchId,
                Row = new StructureWarehouseResponse(
                    w.Id, w.Code, w.NameAr, w.NameEn, w.WarehouseType, w.AllowsSelling, w.IsActive,
                    w.Locations.Count(l => isActive == null || l.IsActive == isActive), w.Locations.Count(l => l.IsActive),
                    w.Locations.Where(l => l.IsDefault).Select(l => l.Code).FirstOrDefault()),
            })
            .ToListAsync(ct);
        var byBranch = warehouses.ToLookup(w => w.BranchId, w => w.Row);
        return branches
            .Select(b => new StructureBranchResponse(b.Id, b.Code, b.NameAr, b.NameEn, b.IsActive, byBranch[b.Id].ToList()))
            .ToList();
    }

    public async Task<PagedResult<LocationDirectoryResponse>> ListLocationsAsync(
        LocationDirectoryFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = Locations(filter.OnlyWarehouseIds);
        if (filter.BranchId is { } branchId)
            query = query.Where(l => l.Warehouse.BranchId == branchId);
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(l => l.WarehouseId == warehouseId);
        if (filter.WarehouseType is { } type)
            query = query.Where(l => l.Warehouse.WarehouseType == type);
        if (filter.Search is { } search)
            query = query.Where(l => l.Code.Contains(search) || l.NameAr.Contains(search) || (l.NameEn != null && l.NameEn.Contains(search)));
        if (filter.IsActive is { } isActive)
            query = query.Where(l => l.IsActive == isActive);

        var total = await query.CountAsync(ct);
        // Grouped by branch and warehouse, the default location first, then the chosen order; ties by id.
        var grouped = query.OrderBy(l => l.Warehouse.Branch.Code).ThenBy(l => l.Warehouse.Code).ThenByDescending(l => l.IsDefault);
        var ordered = (filter.Sort, filter.Descending) switch
        {
            (OrganizationSort.NameAr, false) => grouped.ThenBy(l => l.NameAr).ThenBy(l => l.Code),
            (OrganizationSort.NameAr, true) => grouped.ThenByDescending(l => l.NameAr).ThenBy(l => l.Code),
            (OrganizationSort.NameEn, false) => grouped.ThenBy(l => l.NameEn).ThenBy(l => l.Code),
            (OrganizationSort.NameEn, true) => grouped.ThenByDescending(l => l.NameEn).ThenBy(l => l.Code),
            (_, true) => grouped.ThenByDescending(l => l.Code),
            _ => grouped.ThenBy(l => l.Code),
        };
        var items = await ordered.ThenBy(l => l.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new LocationDirectoryResponse(
                l.Id, l.Code, l.NameAr, l.NameEn, l.IsDefault, l.IsActive,
                l.WarehouseId, l.Warehouse.Code, l.Warehouse.NameAr, l.Warehouse.NameEn, l.Warehouse.WarehouseType, l.Warehouse.IsActive,
                l.Warehouse.BranchId, l.Warehouse.Branch.Code, l.Warehouse.Branch.NameAr, l.Warehouse.Branch.NameEn, l.CreatedAtUtc))
            .ToListAsync(ct);
        return new PagedResult<LocationDirectoryResponse>(items, page, pageSize, total);
    }

    // Problems are judged on all the data (a branch's warehouses outside the caller's scope still count), then only the
    // records the caller may see are listed.
    public async Task<IReadOnlyList<OrganizationDataIssueResponse>> ListDataIssuesAsync(IReadOnlySet<Guid>? scope, CancellationToken ct)
    {
        var issues = new List<OrganizationDataIssueResponse>();
        var company = await db.Set<Company>().AsNoTracking()
            .Select(c => new { c.Id, c.NameAr, c.NameEn, c.TaxRegistrationNumber, c.CommercialRegistrationNumber, c.AddressLine, c.CityId, c.Phone, c.Email })
            .FirstOrDefaultAsync(ct);
        if (company is not null)
        {
            void Company(OrganizationDataIssue issue) =>
                issues.Add(new(issue, OrganizationEntity.Company, company.Id, null, company.NameAr, company.NameEn, null, null, null, null, null));
            if (company.TaxRegistrationNumber is null)
                Company(OrganizationDataIssue.CompanyMissingTaxNumber);
            if (company.CommercialRegistrationNumber is null)
                Company(OrganizationDataIssue.CompanyMissingCommercialRegistration);
            if (company.AddressLine is null || company.CityId is null)
                Company(OrganizationDataIssue.CompanyMissingAddress);
            if (company.Phone is null && company.Email is null)
                Company(OrganizationDataIssue.CompanyMissingContact);
            if (company.NameEn is null)
                Company(OrganizationDataIssue.MissingEnglishName);
        }

        var branches = await db.Set<Branch>().AsNoTracking()
            .Select(b => new { b.Id, b.Code, b.NameAr, b.NameEn, b.IsActive })
            .ToListAsync(ct);
        var warehouses = await db.Set<Warehouse>().AsNoTracking()
            .Select(w => new
            {
                w.Id, w.BranchId, w.Code, w.NameAr, w.NameEn, w.WarehouseType, w.IsActive,
                DefaultIsActive = w.Locations.Where(l => l.IsDefault).Select(l => (bool?)l.IsActive).FirstOrDefault(),
            })
            .ToListAsync(ct);
        var unnamedLocations = await Locations(scope)
            .Where(l => l.NameEn == null && l.Warehouse.WarehouseType == WarehouseType.Physical)
            .Select(l => new { l.Id, l.Code, l.NameAr, l.WarehouseId })
            .ToListAsync(ct);

        bool InScope(Guid warehouseId) => scope is null || scope.Contains(warehouseId);
        var branchById = branches.ToDictionary(b => b.Id);
        var visibleBranches = scope is null
            ? branches.Select(b => b.Id).ToHashSet()
            : warehouses.Where(w => InScope(w.Id)).Select(w => w.BranchId).ToHashSet();
        var warehouseById = warehouses.ToDictionary(w => w.Id);

        foreach (var b in branches.Where(b => visibleBranches.Contains(b.Id)))
        {
            void Branch(OrganizationDataIssue issue, string? detail = null) =>
                issues.Add(new(issue, OrganizationEntity.Branch, b.Id, b.Code, b.NameAr, b.NameEn, b.Id, b.Code, null, null, detail));
            if (b.NameEn is null)
                Branch(OrganizationDataIssue.MissingEnglishName);
            if (!b.IsActive)
                continue;
            var active = warehouses.Where(w => w.BranchId == b.Id && w.IsActive).ToList();
            if (active.Count == 0)
                Branch(OrganizationDataIssue.BranchWithoutActiveWarehouse);
            var transit = active.Count(w => w.WarehouseType == WarehouseType.Transit);
            if (active.Any(w => w.WarehouseType == WarehouseType.Physical) && transit != 1)
                Branch(OrganizationDataIssue.NoDefaultTransitWarehouse, transit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        foreach (var w in warehouses.Where(w => InScope(w.Id)))
        {
            var branch = branchById[w.BranchId];
            void Warehouse(OrganizationDataIssue issue) =>
                issues.Add(new(issue, OrganizationEntity.Warehouse, w.Id, w.Code, w.NameAr, w.NameEn, branch.Id, branch.Code, w.Id, w.Code, null));
            if (w.NameEn is null)
                Warehouse(OrganizationDataIssue.MissingEnglishName);
            if (w.IsActive && !branch.IsActive)
                Warehouse(OrganizationDataIssue.ActiveWarehouseInInactiveBranch);
            if (w.DefaultIsActive is null)
                Warehouse(OrganizationDataIssue.NoDefaultLocation);
            else if (w.DefaultIsActive == false)
                Warehouse(OrganizationDataIssue.InactiveDefaultLocation);
        }

        foreach (var l in unnamedLocations)
        {
            var w = warehouseById[l.WarehouseId];
            var branch = branchById[w.BranchId];
            issues.Add(new(OrganizationDataIssue.MissingEnglishName, OrganizationEntity.Location, l.Id, l.Code, l.NameAr, null,
                branch.Id, branch.Code, w.Id, w.Code, null));
        }

        return issues
            .OrderBy(x => x.Issue).ThenBy(x => x.Entity)
            .ThenBy(x => x.BranchCode, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.WarehouseCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IQueryable<Warehouse> Warehouses(IReadOnlySet<Guid>? scope)
    {
        var query = db.Set<Warehouse>().AsNoTracking();
        if (scope is null)
            return query;
        var ids = scope.ToArray(); // a parameter collection EF Core translates
        return query.Where(w => ids.Contains(w.Id));
    }

    private IQueryable<Branch> Branches(IReadOnlySet<Guid>? scope)
    {
        var query = db.Set<Branch>().AsNoTracking();
        if (scope is null)
            return query;
        var ids = scope.ToArray();
        return query.Where(b => b.Warehouses.Any(w => ids.Contains(w.Id)));
    }

    private IQueryable<WarehouseLocation> Locations(IReadOnlySet<Guid>? scope)
    {
        var query = db.Set<WarehouseLocation>().AsNoTracking();
        if (scope is null)
            return query;
        var ids = scope.ToArray();
        return query.Where(l => ids.Contains(l.WarehouseId));
    }
}
