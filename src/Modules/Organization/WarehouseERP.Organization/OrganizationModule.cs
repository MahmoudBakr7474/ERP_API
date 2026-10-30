using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Contracts.Companies;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.Branches.CreateBranch;
using WarehouseERP.Organization.Application.Features.Branches.GetBranch;
using WarehouseERP.Organization.Application.Features.Branches.ListBranches;
using WarehouseERP.Organization.Application.Features.Branches.SetBranchActive;
using WarehouseERP.Organization.Application.Features.Branches.UpdateBranch;
using WarehouseERP.Organization.Application.Features.Companies.GetCompany;
using WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;
using WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;
using WarehouseERP.Organization.Application.Features.Exports.ExportBranches;
using WarehouseERP.Organization.Application.Features.Exports.ExportDataIssues;
using WarehouseERP.Organization.Application.Features.Exports.ExportLocations;
using WarehouseERP.Organization.Application.Features.Exports.ExportStructure;
using WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;
using WarehouseERP.Organization.Application.Features.Geography.CreateCity;
using WarehouseERP.Organization.Application.Features.Geography.CreateCountry;
using WarehouseERP.Organization.Application.Features.Geography.CreateGovernorate;
using WarehouseERP.Organization.Application.Features.Geography.ListCities;
using WarehouseERP.Organization.Application.Features.Geography.ListCountries;
using WarehouseERP.Organization.Application.Features.Geography.ListGovernorates;
using WarehouseERP.Organization.Application.Features.Geography.SetCityActive;
using WarehouseERP.Organization.Application.Features.Geography.SetCountryActive;
using WarehouseERP.Organization.Application.Features.Geography.SetGovernorateActive;
using WarehouseERP.Organization.Application.Features.Geography.UpdateCity;
using WarehouseERP.Organization.Application.Features.Geography.UpdateCountry;
using WarehouseERP.Organization.Application.Features.Geography.UpdateGovernorate;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;
using WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;
using WarehouseERP.Organization.Application.Features.Imports.PreviewLocationImport;
using WarehouseERP.Organization.Application.Features.Reports.GetOrganizationStructure;
using WarehouseERP.Organization.Application.Features.Reports.GetOrganizationSummary;
using WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.GetWarehouseLocation;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.ListLocationDirectory;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.ListWarehouseLocations;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.UpdateWarehouseLocation;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;
using WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;
using WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Infrastructure;
using WarehouseERP.Organization.Infrastructure.Persistence;
using WarehouseERP.Organization.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization;

// Companies, branches, warehouses and their locations, and the address lookups (countries, governorates, cities). The host calls AddOrganizationModule and
// MapOrganizationEndpoints; nothing else of this module is public to it.
public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateWarehouseValidator>(includeInternalTypes: true);

        services.AddQueryHandler<GetCompanyQuery, CompanyResponse, GetCompanyHandler>();
        services.AddCommandHandler<SetUpCompanyCommand, Guid, SetUpCompanyHandler>();
        services.AddCommandHandler<UpdateCompanyCommand, string, UpdateCompanyHandler>();

        services.AddQueryHandler<ListBranchesQuery, PagedResult<BranchSummaryResponse>, ListBranchesHandler>();
        services.AddQueryHandler<GetBranchQuery, BranchDetailsResponse, GetBranchHandler>();
        services.AddCommandHandler<CreateBranchCommand, Guid, CreateBranchHandler>();
        services.AddCommandHandler<UpdateBranchCommand, string, UpdateBranchHandler>();
        services.AddCommandHandler<SetBranchActiveCommand, Done, SetBranchActiveHandler>();

        services.AddCommandHandler<CreateWarehouseCommand, Guid, CreateWarehouseHandler>();
        services.AddQueryHandler<GetWarehouseQuery, WarehouseDetailsResponse, GetWarehouseHandler>();
        services.AddQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummaryResponse>, ListWarehousesHandler>();
        services.AddCommandHandler<UpdateWarehouseCommand, string, UpdateWarehouseHandler>();
        services.AddCommandHandler<SetWarehouseActiveCommand, Done, SetWarehouseActiveHandler>();

        services.AddQueryHandler<ListWarehouseLocationsQuery, PagedResult<WarehouseLocationResponse>, ListWarehouseLocationsHandler>();
        services.AddQueryHandler<GetWarehouseLocationQuery, WarehouseLocationResponse, GetWarehouseLocationHandler>();
        services.AddCommandHandler<CreateWarehouseLocationCommand, Guid, CreateWarehouseLocationHandler>();
        services.AddCommandHandler<UpdateWarehouseLocationCommand, string, UpdateWarehouseLocationHandler>();
        services.AddCommandHandler<SetWarehouseLocationActiveCommand, Done, SetWarehouseLocationActiveHandler>();

        services.AddQueryHandler<ListLocationDirectoryQuery, PagedResult<LocationDirectoryResponse>, ListLocationDirectoryHandler>();
        services.AddQueryHandler<GetOrganizationSummaryQuery, OrganizationSummaryResponse, GetOrganizationSummaryHandler>();
        services.AddQueryHandler<GetOrganizationStructureQuery, IReadOnlyList<StructureBranchResponse>, GetOrganizationStructureHandler>();
        services.AddQueryHandler<ListOrganizationDataIssuesQuery, OrganizationDataQualityResponse, ListOrganizationDataIssuesHandler>();
        services.AddQueryHandler<ExportBranchesQuery, IReadOnlyList<BranchSummaryResponse>, ExportBranchesHandler>();
        services.AddQueryHandler<ExportWarehousesQuery, IReadOnlyList<WarehouseSummaryResponse>, ExportWarehousesHandler>();
        services.AddQueryHandler<ExportLocationsQuery, IReadOnlyList<LocationDirectoryResponse>, ExportLocationsHandler>();
        services.AddQueryHandler<ExportStructureQuery, OrganizationStructureExport, ExportStructureHandler>();
        services.AddQueryHandler<ExportDataIssuesQuery, IReadOnlyList<OrganizationDataIssueResponse>, ExportDataIssuesHandler>();
        services.AddQueryHandler<PreviewLocationImportQuery, LocationImportPreviewResponse, PreviewLocationImportHandler>();
        services.AddCommandHandler<CommitLocationImportCommand, LocationImportResultResponse, CommitLocationImportHandler>();

        services.AddQueryHandler<ListCountriesQuery, IReadOnlyList<CountryResponse>, ListCountriesHandler>();
        services.AddCommandHandler<CreateCountryCommand, Guid, CreateCountryHandler>();
        services.AddCommandHandler<UpdateCountryCommand, string, UpdateCountryHandler>();
        services.AddCommandHandler<SetCountryActiveCommand, Done, SetCountryActiveHandler>();
        services.AddQueryHandler<ListGovernoratesQuery, IReadOnlyList<GovernorateResponse>, ListGovernoratesHandler>();
        services.AddCommandHandler<CreateGovernorateCommand, Guid, CreateGovernorateHandler>();
        services.AddCommandHandler<UpdateGovernorateCommand, string, UpdateGovernorateHandler>();
        services.AddCommandHandler<SetGovernorateActiveCommand, Done, SetGovernorateActiveHandler>();
        services.AddQueryHandler<ListCitiesQuery, IReadOnlyList<CityResponse>, ListCitiesHandler>();
        services.AddCommandHandler<CreateCityCommand, Guid, CreateCityHandler>();
        services.AddCommandHandler<UpdateCityCommand, string, UpdateCityHandler>();
        services.AddCommandHandler<SetCityActiveCommand, Done, SetCityActiveHandler>();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyReadStore, CompanyReadStore>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBranchReadStore, BranchReadStore>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseReadStore, WarehouseReadStore>();
        services.AddScoped<IOrganizationReportReadStore, OrganizationReportReadStore>();
        services.AddScoped<ILocationImportStore, LocationImportStore>();
        services.AddScoped<WarehouseDirectory>();
        services.AddScoped<IWarehouseDirectory>(sp => sp.GetRequiredService<WarehouseDirectory>());
        services.AddScoped<IBranchDirectory>(sp => sp.GetRequiredService<WarehouseDirectory>());
        services.AddScoped<IWarehouseListing>(sp => sp.GetRequiredService<WarehouseDirectory>());
        services.AddScoped<IWarehouseLocationDirectory, WarehouseLocationDirectory>();
        services.AddScoped<ICompanyDirectory, CompanyDirectory>();
        services.AddScoped<IDocumentIssuerSource, DocumentIssuerSource>();
        services.AddScoped<ITransitLocations, TransitLocations>();
        services.AddScoped<IGeographyRepository, GeographyRepository>();
        services.AddScoped<IGeographyReadStore, GeographyReadStore>();
        services.AddScoped<IGeographyDirectory, GeographyDirectory>();

        services.AddPermissions("Organization", OrganizationPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app) =>
        app.MapCompanyEndpoints()
            .MapBranchEndpoints()
            .MapWarehouseEndpoints()
            .MapWarehouseLocationEndpoints()
            .MapGeographyEndpoints()
            .MapOrganizationReportEndpoints()
            .MapOrganizationExportEndpoints()
            .MapLocationImportEndpoints();
}
