using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Features.Branches.CreateBranch;
using WarehouseERP.Organization.Application.Features.Branches.GetBranch;
using WarehouseERP.Organization.Application.Features.Branches.ListBranches;
using WarehouseERP.Organization.Application.Features.Branches.SetBranchActive;
using WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;
using WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;
using WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;
using WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.Application;

// Company, branch, warehouse-change and location handlers against in-memory fakes of their ports.
public sealed class OrganizationHandlerTests
{
    private const string RowVersion = "AAAAAAAAB9E=";
    private static readonly FakeUser CompanyWide = new(allWarehouses: true);

    private static Company NewCompany() => Company.Create("CO", "الشركة", null, "egp", "Africa/Cairo", null);

    private static Warehouse NewWarehouse(WarehouseType type = WarehouseType.Physical) =>
        Warehouse.Create(Guid.NewGuid(), "WH-1", "مخزن", null, type, allowsSelling: false).Value;

    [Fact]
    public async Task Set_up_company_needs_company_wide_access_and_passes_an_existing_company_through()
    {
        var command = new SetUpCompanyCommand("CO", "الشركة", null, "egp", "Africa/Cairo", null);
        var companies = new FakeCompanyRepository();

        var refused = await new SetUpCompanyHandler(companies, new FakeUser(false), NullLogger<SetUpCompanyHandler>.Instance)
            .HandleAsync(command, default);
        Assert.Same(CompanyErrors.CompanyWideAccessRequired, refused.Error);
        Assert.Null(companies.Added);

        var created = await new SetUpCompanyHandler(companies, CompanyWide, NullLogger<SetUpCompanyHandler>.Instance).HandleAsync(command, default);
        Assert.True(created.IsSuccess);
        Assert.Equal("EGP", companies.Added!.BaseCurrencyCode);

        var existing = new FakeCompanyRepository { SaveResult = CompanyErrors.AlreadyConfigured };
        var second = await new SetUpCompanyHandler(existing, CompanyWide, NullLogger<SetUpCompanyHandler>.Instance).HandleAsync(command, default);
        Assert.Same(CompanyErrors.AlreadyConfigured, second.Error);
    }

    [Fact]
    public async Task Update_company_compares_the_callers_row_version_and_is_not_found_before_setup()
    {
        var command = new UpdateCompanyCommand("اسم جديد", null, "Africa/Cairo", "123", RowVersion);

        var missing = await new UpdateCompanyHandler(new FakeCompanyRepository(), CompanyWide, NullLogger<UpdateCompanyHandler>.Instance)
            .HandleAsync(command, default);
        Assert.Same(CompanyErrors.NotConfigured, missing.Error);

        var company = NewCompany();
        var companies = new FakeCompanyRepository { Stored = company, SaveResult = CommonErrors.ConcurrencyConflict };
        var result = await new UpdateCompanyHandler(companies, CompanyWide, NullLogger<UpdateCompanyHandler>.Instance).HandleAsync(command, default);

        Assert.Same(CommonErrors.ConcurrencyConflict, result.Error);
        Assert.Equal(("اسم جديد", "CO", "EGP"), (company.NameAr, company.Code, company.BaseCurrencyCode));
        Assert.Equal(Convert.FromBase64String(RowVersion), companies.ExpectedRowVersion);
    }

    [Fact]
    public async Task Create_branch_needs_company_wide_access_a_set_up_company_and_a_free_code()
    {
        var command = new CreateBranchCommand(" BR-1 ", "فرع", null);
        CreateBranchHandler Handler(FakeCompanyRepository companies, FakeBranchRepository branches, ICurrentUser user) =>
            new(companies, branches, user, NullLogger<CreateBranchHandler>.Instance);

        Assert.Same(CompanyErrors.CompanyWideAccessRequired,
            (await Handler(new FakeCompanyRepository { Stored = NewCompany() }, new FakeBranchRepository(), new FakeUser(false))
                .HandleAsync(command, default)).Error);
        Assert.Same(CompanyErrors.SetupRequired,
            (await Handler(new FakeCompanyRepository(), new FakeBranchRepository(), CompanyWide).HandleAsync(command, default)).Error);
        Assert.Same(BranchErrors.CodeTaken,
            (await Handler(new FakeCompanyRepository { Stored = NewCompany() }, new FakeBranchRepository { CodeExists = true }, CompanyWide)
                .HandleAsync(command, default)).Error);

        var company = NewCompany();
        var branches = new FakeBranchRepository();
        var created = await Handler(new FakeCompanyRepository { Stored = company }, branches, CompanyWide).HandleAsync(command, default);

        Assert.True(created.IsSuccess);
        Assert.Equal("BR-1", branches.Added!.Code);
        Assert.Same(branches.Added, Assert.Single(company.Branches));
    }

    [Fact]
    public async Task A_branch_with_active_warehouses_is_not_deactivated()
    {
        var branch = NewCompany().AddBranch("BR-1", "فرع", null);
        var branches = new FakeBranchRepository { Stored = branch, HasActiveWarehouses = true };

        var result = await new SetBranchActiveHandler(branches, CompanyWide, NullLogger<SetBranchActiveHandler>.Instance)
            .HandleAsync(new SetBranchActiveCommand(branch.Id, IsActive: false), default);

        Assert.Same(BranchErrors.HasActiveWarehouses, result.Error);
        Assert.True(branch.IsActive);
        Assert.Equal(0, branches.Saves);
    }

    [Fact]
    public async Task Get_branch_checks_scope_through_the_users_warehouses_before_reading()
    {
        var store = new FakeBranchReadStore();
        var warehouse = Guid.NewGuid();

        var refused = await new GetBranchHandler(store, new FakeUser(false, warehouse)).HandleAsync(new GetBranchQuery(Guid.NewGuid()), default);
        Assert.Same(BranchErrors.AccessDenied, refused.Error);
        Assert.Equal([warehouse], store.LastScope!);
        Assert.Equal(0, store.Reads);

        var missing = await new GetBranchHandler(store, CompanyWide).HandleAsync(new GetBranchQuery(Guid.NewGuid()), default);
        Assert.Same(BranchErrors.NotFound, missing.Error);
    }

    [Fact]
    public async Task List_branches_restricts_to_the_users_warehouses_and_trims_the_search()
    {
        var store = new FakeBranchReadStore();
        var warehouse = Guid.NewGuid();
        var options = Options.Create(new PaginationOptions());

        await new ListBranchesHandler(store, new FakeUser(false, warehouse), options).HandleAsync(new ListBranchesQuery(1, null, "  ", true), default);
        Assert.Equal([warehouse], store.LastFilter!.ThroughWarehouseIds!);
        Assert.Equal((null, true), (store.LastFilter.Search, store.LastFilter.IsActive));

        await new ListBranchesHandler(store, CompanyWide, options).HandleAsync(new ListBranchesQuery(1, null, " Cairo ", null), default);
        Assert.Equal((null, "Cairo"), (store.LastFilter!.ThroughWarehouseIds, store.LastFilter.Search));
    }

    [Fact]
    public async Task Reactivating_a_warehouse_needs_an_active_branch_and_scope_comes_first()
    {
        var warehouse = NewWarehouse();
        warehouse.Deactivate();
        var warehouses = new FakeWarehouseRepository { Stored = warehouse, BranchActive = false };

        var outOfScope = await new SetWarehouseActiveHandler(warehouses, new FakeUser(false), NullLogger<SetWarehouseActiveHandler>.Instance)
            .HandleAsync(new SetWarehouseActiveCommand(warehouse.Id, IsActive: true), default);
        Assert.Same(WarehouseErrors.AccessDenied, outOfScope.Error);
        Assert.Equal(0, warehouses.Loads);

        var result = await new SetWarehouseActiveHandler(warehouses, CompanyWide, NullLogger<SetWarehouseActiveHandler>.Instance)
            .HandleAsync(new SetWarehouseActiveCommand(warehouse.Id, IsActive: true), default);
        Assert.Same(BranchErrors.NotActive, result.Error);
        Assert.False(warehouse.IsActive);
    }

    [Fact]
    public async Task Update_warehouse_keeps_a_transit_warehouse_from_selling_without_saving()
    {
        var warehouses = new FakeWarehouseRepository { Stored = NewWarehouse(WarehouseType.Transit) };

        var result = await new UpdateWarehouseHandler(warehouses, CompanyWide, NullLogger<UpdateWarehouseHandler>.Instance)
            .HandleAsync(new UpdateWarehouseCommand(Guid.NewGuid(), "ترانزيت", null, AllowsSelling: true, RowVersion), default);

        Assert.Same(WarehouseDomainErrors.TransitCannotSell, result.Error);
        Assert.Equal(0, warehouses.Saves);
    }

    [Fact]
    public async Task Create_location_adds_it_to_an_active_warehouse_once_and_refuses_an_inactive_one()
    {
        var command = new CreateWarehouseLocationCommand(Guid.NewGuid(), "A-01", "رف", null);
        var warehouse = NewWarehouse();
        var warehouses = new FakeWarehouseRepository { Stored = warehouse };

        var created = await new CreateWarehouseLocationHandler(warehouses, CompanyWide, NullLogger<CreateWarehouseLocationHandler>.Instance)
            .HandleAsync(command, default);
        Assert.True(created.IsSuccess);
        Assert.Equal(1, warehouses.Saves);
        Assert.Contains(warehouse.Locations, l => l is { Code: "A-01", IsDefault: false });

        var taken = await new CreateWarehouseLocationHandler(new FakeWarehouseRepository { Stored = NewWarehouse(), LocationCodeExists = true },
            CompanyWide, NullLogger<CreateWarehouseLocationHandler>.Instance).HandleAsync(command, default);
        Assert.Same(WarehouseLocationErrors.CodeTaken, taken.Error);

        var inactive = NewWarehouse();
        inactive.Deactivate();
        var refused = await new CreateWarehouseLocationHandler(new FakeWarehouseRepository { Stored = inactive }, CompanyWide,
            NullLogger<CreateWarehouseLocationHandler>.Instance).HandleAsync(command, default);
        Assert.Same(WarehouseDomainErrors.NotActive, refused.Error);
    }

    [Fact]
    public async Task The_default_location_is_not_deactivated()
    {
        var location = Assert.Single(NewWarehouse().Locations);
        var warehouses = new FakeWarehouseRepository { StoredLocation = location };

        var result = await new SetWarehouseLocationActiveHandler(warehouses, CompanyWide, NullLogger<SetWarehouseLocationActiveHandler>.Instance)
            .HandleAsync(new SetWarehouseLocationActiveCommand(Guid.NewGuid(), location.Id, IsActive: false), default);

        Assert.Same(WarehouseLocationDomainErrors.DefaultMustStayActive, result.Error);
        Assert.True(location.IsActive);
        Assert.Equal(0, warehouses.Saves);
    }

    private sealed class FakeUser(bool allWarehouses, params Guid[] warehouses) : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
        public bool HasAllWarehouses => allWarehouses;
        public IReadOnlySet<Guid> WarehouseIds { get; } = warehouses.ToHashSet();
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public Company? Stored { get; init; }
        public Result SaveResult { get; init; } = Result.Success();
        public Company? Added { get; private set; }
        public byte[]? ExpectedRowVersion { get; private set; }

        public Task<Result> AddIfNoneAsync(Company company, CancellationToken ct)
        {
            Added = company;
            return Task.FromResult(SaveResult);
        }

        public Task<Company?> GetForUpdateAsync(byte[]? expectedRowVersion, CancellationToken ct)
        {
            ExpectedRowVersion = expectedRowVersion;
            return Task.FromResult(Stored);
        }

        public Task<Result> SaveChangesAsync(CancellationToken ct) => Task.FromResult(SaveResult);
    }

    private sealed class FakeBranchRepository : IBranchRepository
    {
        public bool CodeExists { get; init; }
        public bool HasActiveWarehouses { get; init; }
        public Branch? Stored { get; init; }
        public Branch? Added { get; private set; }
        public int Saves { get; private set; }

        public Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct) => Task.FromResult(CodeExists);

        public Task<Result> AddAsync(Branch branch, CancellationToken ct)
        {
            Added = branch;
            Saves++;
            return Task.FromResult(Result.Success());
        }

        public Task<Branch?> GetForUpdateAsync(Guid branchId, byte[]? expectedRowVersion, CancellationToken ct) => Task.FromResult(Stored);

        public Task<bool> HasActiveWarehousesAsync(Guid branchId, CancellationToken ct) => Task.FromResult(HasActiveWarehouses);

        public Task<Result> SaveChangesAsync(CancellationToken ct)
        {
            Saves++;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FakeBranchReadStore : IBranchReadStore
    {
        public int Reads { get; private set; }
        public IReadOnlySet<Guid>? LastScope { get; private set; }
        public BranchListFilter? LastFilter { get; private set; }

        public Task<BranchDetailsResponse?> GetAsync(Guid branchId, CancellationToken ct)
        {
            Reads++;
            return Task.FromResult<BranchDetailsResponse?>(null);
        }

        public Task<PagedResult<BranchSummaryResponse>> ListAsync(BranchListFilter filter, int page, int pageSize, CancellationToken ct)
        {
            LastFilter = filter;
            return Task.FromResult(new PagedResult<BranchSummaryResponse>([], page, pageSize, 0));
        }

        public Task<bool> HoldsAnyWarehouseAsync(Guid branchId, IReadOnlySet<Guid> warehouseIds, CancellationToken ct)
        {
            LastScope = warehouseIds;
            return Task.FromResult(false);
        }
    }

    private sealed class FakeWarehouseRepository : IWarehouseRepository
    {
        public Warehouse? Stored { get; init; }
        public WarehouseLocation? StoredLocation { get; init; }
        public bool BranchActive { get; init; } = true;
        public bool LocationCodeExists { get; init; }
        public int Loads { get; private set; }
        public int Saves { get; private set; }

        public Task<bool> IsBranchActiveAsync(Guid branchId, CancellationToken ct) => Task.FromResult(BranchActive);
        public Task<bool> CodeExistsAsync(Guid branchId, string code, CancellationToken ct) => Task.FromResult(false);
        public Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct) => Task.FromResult(Result.Success());

        public Task<Warehouse?> GetForUpdateAsync(Guid warehouseId, byte[]? expectedRowVersion, CancellationToken ct)
        {
            Loads++;
            return Task.FromResult(Stored);
        }

        public Task<bool> LocationCodeExistsAsync(Guid warehouseId, string code, CancellationToken ct) => Task.FromResult(LocationCodeExists);

        public Task<WarehouseLocation?> GetLocationForUpdateAsync(Guid warehouseId, Guid locationId, byte[]? expectedRowVersion, CancellationToken ct) =>
            Task.FromResult(StoredLocation);

        public Task<Result> SaveChangesAsync(CancellationToken ct)
        {
            Saves++;
            return Task.FromResult(Result.Success());
        }
    }
}
