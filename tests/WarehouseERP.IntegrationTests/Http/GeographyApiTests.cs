using System.Net;
using System.Text.Json;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// Countries, governorates and cities (Migration08 seeds every country and Egypt's governorates and cities), and the
// supplier addresses that name them. The tests share one database, so changes happen on records each test creates.
[Collection(DatabaseCollection.Name)]
public sealed class GeographyApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Countries = "/api/organization/countries";
    private const string Governorates = "/api/organization/governorates";
    private const string Cities = "/api/organization/cities";
    private static readonly string[] SomeSeededGovernorates = ["الإسكندرية", "أسوان", "مطروح", "الوادي الجديد"];
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        await fx.EnsureCompanyAsync();
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(PurchasingTesting.AllPermissions);

    private static JsonElement Named(JsonElement list, string nameAr) =>
        list.EnumerateArray().Single(x => x.GetProperty("nameAr").GetString() == nameAr);

    private async Task<Guid> SeededCityAsync(string governorate, string city)
    {
        var client = Admin();
        var governorateId = Named(await client.GetJsonAsync($"{Governorates}?countryCode=EG"), governorate).GetProperty("id").GetGuid();
        return Named(await client.GetJsonAsync($"{Cities}?governorateId={governorateId}"), city).GetProperty("id").GetGuid();
    }

    // A governorate and city of their own under Egypt, so deactivating them touches no other test.
    private async Task<(Guid Governorate, Guid City)> OwnCityAsync()
    {
        var client = Admin();
        var governorate = await client.PostJsonAsync(Governorates, new { countryCode = "EG", nameAr = DatabaseFixture.Code("محافظة"), nameEn = "Test" })
            .CreatedIdAsync();
        var city = await client.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = DatabaseFixture.Code("مدينة"), nameEn = (string?)null })
            .CreatedIdAsync();
        return (governorate, city);
    }

    private static object Address(Guid cityId, string line1 = "شارع التحرير") =>
        new { addressType = "Billing", line1, line2 = (string?)null, cityId, postalCode = (string?)null, isPrimary = false };

    private static object Supplier(string code, string countryCode, params object[] addresses) => new
    {
        code, nameAr = "مورد العناوين", nameEn = (string?)null, countryCode, taxRegistrationNumber = (string?)null,
        commercialRegistrationNumber = (string?)null, commercialRegistry = (string?)null, paymentTermDays = 0, phone = (string?)null,
        email = (string?)null, notes = (string?)null, contacts = Array.Empty<object>(), addresses,
    };

    [Fact]
    public async Task Seeded_lookups_cascade_from_country_to_governorate_to_city()
    {
        var reader = _api.CreateClient().As(permissions: "");

        var countries = await reader.GetJsonAsync(Countries);
        var egypt = Named(countries, "مصر");
        var governorates = await reader.GetJsonAsync($"{Governorates}?countryCode=eg&isActive=true");
        var cairo = Named(governorates, "القاهرة");
        var cities = await reader.GetJsonAsync($"{Cities}?governorateId={cairo.GetProperty("id").GetGuid()}");

        Assert.True(countries.GetArrayLength() >= 249);
        Assert.Equal(("EG", "Egypt"), (egypt.GetProperty("code").GetString(), egypt.GetProperty("nameEn").GetString()));
        // Other tests add governorates of their own under Egypt; the 27 seeded ones are all there.
        Assert.True(governorates.GetArrayLength() >= 27);
        Assert.All(SomeSeededGovernorates, name => Named(governorates, name));
        Assert.Equal("EG", cairo.GetProperty("countryCode").GetString());
        Assert.Equal("Nasr City", Named(cities, "مدينة نصر").GetProperty("nameEn").GetString());
        Assert.Equal(0, (await reader.GetJsonAsync($"{Governorates}?countryCode=SA")).GetArrayLength());
    }

    [Fact]
    public async Task Lookups_need_a_signed_in_user_and_a_known_parent()
    {
        var reader = _api.CreateClient().As(permissions: "");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync(Countries)).StatusCode);
        await (await reader.GetAsync($"{Governorates}?countryCode=QZ")).AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Country.NotFound");
        await (await reader.GetAsync($"{Governorates}?countryCode=EGY")).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await reader.GetAsync(Cities)).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await reader.GetAsync($"{Cities}?governorateId={Guid.NewGuid()}"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Governorate.NotFound");
    }

    [Fact]
    public async Task Managing_lookups_needs_the_permission_and_company_wide_access()
    {
        var (governorate, _) = await OwnCityAsync();
        var noPermission = _api.CreateClient().As(string.Join(',', OrganizationPermissions.All.Where(p => p != OrganizationPermissions.GeographyManage)));
        var scoped = _api.CreateClient().As(OrganizationPermissions.GeographyManage, warehouses: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await noPermission.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = "مدينة" })).StatusCode);
        await (await scoped.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = "مدينة" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
        await (await scoped.PostAsync($"{Governorates}/{governorate}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
    }

    [Fact]
    public async Task Names_are_unique_under_their_parent_and_renames_need_the_current_row_version()
    {
        var client = Admin();
        var (governorate, city) = await OwnCityAsync();
        var name = DatabaseFixture.Code("حي");
        await client.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = name }).CreatedIdAsync();

        await (await client.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = $" {name} " }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.City.NameTaken");
        await (await client.PostJsonAsync(Governorates, new { countryCode = "EG", nameAr = "القاهرة" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Governorate.NameTaken");
        await (await client.PostJsonAsync(Countries, new { code = "eg", nameAr = "دولة" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Country.CodeTaken");
        await (await client.PostJsonAsync(Countries, new { code = "E1", nameAr = "دولة" }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");

        var current = (await client.GetJsonAsync($"{Cities}?governorateId={governorate}")).EnumerateArray()
            .Single(x => x.GetProperty("id").GetGuid() == city);
        var renamed = await client.PutJsonAsync($"{Cities}/{city}", new { nameAr = DatabaseFixture.Code("اسم"), nameEn = "Renamed", rowVersion = current.GetProperty("rowVersion").GetString() });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        await (await client.PutJsonAsync($"{Cities}/{city}", new { nameAr = "قديم", rowVersion = current.GetProperty("rowVersion").GetString() }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
    }

    [Fact]
    public async Task A_new_country_takes_governorates_and_cities_and_an_inactive_parent_takes_none()
    {
        var client = Admin();
        var country = await client.PostJsonAsync(Countries, new { code = "xa", nameAr = DatabaseFixture.Code("دولة"), nameEn = "Testland" }).CreatedIdAsync();
        var governorate = await client.PostJsonAsync(Governorates, new { countryCode = "XA", nameAr = "إقليم" }).CreatedIdAsync();

        Assert.Equal("XA", Named(await client.GetJsonAsync($"{Governorates}?countryCode=XA"), "إقليم").GetProperty("countryCode").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Governorates}/{governorate}/deactivate", null)).StatusCode);
        await (await client.PostJsonAsync(Cities, new { governorateId = governorate, nameAr = "مدينة" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Governorate.NotActive");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Countries}/{country}/deactivate", null)).StatusCode);
        await (await client.PostJsonAsync(Governorates, new { countryCode = "XA", nameAr = "إقليم آخر" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Country.NotActive");
        Assert.Equal(0, (await client.GetJsonAsync($"{Governorates}?countryCode=XA&isActive=true")).GetArrayLength());
    }

    [Fact]
    public async Task A_supplier_address_names_a_city_and_reads_back_its_governorate_and_country()
    {
        var client = Admin();
        var nasrCity = await SeededCityAsync("القاهرة", "مدينة نصر");

        var id = await client.PostJsonAsync(PurchasingTesting.Suppliers, Supplier(DatabaseFixture.Code("S"), "eg", Address(nasrCity)))
            .CreatedIdAsync();
        var address = (await client.GetJsonAsync($"{PurchasingTesting.Suppliers}/{id}")).GetProperty("addresses")[0];

        Assert.Equal((nasrCity, "مدينة نصر", "القاهرة", "EG", "مصر"),
            (address.GetProperty("cityId").GetGuid(), address.GetProperty("cityNameAr").GetString(), address.GetProperty("governorateNameAr").GetString(),
             address.GetProperty("countryCode").GetString(), address.GetProperty("countryNameAr").GetString()));
    }

    [Fact]
    public async Task A_supplier_needs_a_listed_active_country_and_active_cities()
    {
        var client = Admin();
        var (_, city) = await OwnCityAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Cities}/{city}/deactivate", null)).StatusCode);

        await (await client.PostJsonAsync(PurchasingTesting.Suppliers, Supplier(DatabaseFixture.Code("S"), "QZ")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.CountryNotFound");
        var inactive = await (await client.PostJsonAsync(PurchasingTesting.Suppliers,
                Supplier(DatabaseFixture.Code("S"), "EG", Address(await SeededCityAsync("الجيزة", "الدقي"), "1"), Address(city, "2"))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.CityNotFound");
        Assert.StartsWith("Address 2:", inactive.GetProperty("detail").GetString());
        await (await client.PostJsonAsync(PurchasingTesting.Suppliers, Supplier(DatabaseFixture.Code("S"), "EG", Address(Guid.NewGuid()))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.CityNotFound");
    }

    [Fact]
    public async Task A_supplier_keeps_a_city_that_was_deactivated_after_it_was_chosen()
    {
        var client = Admin();
        var (governorate, city) = await OwnCityAsync();
        var code = DatabaseFixture.Code("S");
        var id = await client.PostJsonAsync(PurchasingTesting.Suppliers, Supplier(code, "EG", Address(city))).CreatedIdAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Governorates}/{governorate}/deactivate", null)).StatusCode);

        var supplier = await client.GetJsonAsync($"{PurchasingTesting.Suppliers}/{id}");
        var updated = await client.PutJsonAsync($"{PurchasingTesting.Suppliers}/{id}", new
        {
            nameAr = "مورد معدل", nameEn = (string?)null, countryCode = "EG", taxRegistrationNumber = (string?)null,
            commercialRegistrationNumber = (string?)null, commercialRegistry = (string?)null, paymentTermDays = 15, phone = (string?)null,
            email = (string?)null, notes = (string?)null, contacts = Array.Empty<object>(), addresses = new[] { Address(city, "عنوان جديد") },
            rowVersion = supplier.GetProperty("rowVersion").GetString(),
        });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        await (await client.PostJsonAsync(PurchasingTesting.Suppliers, Supplier(DatabaseFixture.Code("S"), "EG", Address(city))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.CityNotFound");
    }
}
