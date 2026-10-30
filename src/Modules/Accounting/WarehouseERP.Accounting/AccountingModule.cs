using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Contracts.FiscalYears;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Features.AccountMappings.ListAccountMappings;
using WarehouseERP.Accounting.Application.Features.AccountMappings.RemoveAccountMapping;
using WarehouseERP.Accounting.Application.Features.AccountMappings.SetAccountMapping;
using WarehouseERP.Accounting.Application.Features.Accounts.ApplyDefaultChart;
using WarehouseERP.Accounting.Application.Features.Accounts.CreateAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.DeleteAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.GetAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.GetAccountTree;
using WarehouseERP.Accounting.Application.Features.Accounts.ImportChartOfAccounts;
using WarehouseERP.Accounting.Application.Features.Accounts.ListAccounts;
using WarehouseERP.Accounting.Application.Features.Accounts.SetAccountActive;
using WarehouseERP.Accounting.Application.Features.Accounts.UpdateAccount;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalPeriod;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CreateFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.GetFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ListFiscalYears;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalPeriod;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalYear;
using WarehouseERP.Accounting.Application.Features.Journals;
using WarehouseERP.Accounting.Application.Features.Journals.ApproveJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CancelJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CreateJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CreateOpeningJournal;
using WarehouseERP.Accounting.Application.Features.Journals.GetJournal;
using WarehouseERP.Accounting.Application.Features.Journals.ListJournals;
using WarehouseERP.Accounting.Application.Features.Journals.PostJournal;
using WarehouseERP.Accounting.Application.Features.Journals.RejectJournal;
using WarehouseERP.Accounting.Application.Features.Journals.ReverseJournal;
using WarehouseERP.Accounting.Application.Features.Journals.SubmitJournal;
using WarehouseERP.Accounting.Application.Features.Journals.UpdateJournal;
using WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;
using WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;
using WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;
using WarehouseERP.Accounting.Application.Features.Settings.GetAccountingSettings;
using WarehouseERP.Accounting.Application.Features.Settings.UpdateAccountingSettings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Infrastructure.Persistence;
using WarehouseERP.Accounting.Infrastructure.ReadModels;
using WarehouseERP.Accounting.Presentation;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting;

// The general ledger (docs/modules/Accounting and ADR 0020): the chart of accounts and its posting-role mappings, fiscal
// years and periods, manual, opening-balance, reversal and year-end closing journals, and the reports computed from posted
// lines. The company and branches come from Organization, display names from Identity. The host calls AddAccountingModule
// and MapAccountingEndpoints; ITransactionRunner comes from Persistence.
public static class AccountingModule
{
    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateJournalValidator>(includeInternalTypes: true);

        services.AddQueryHandler<GetAccountingSettingsQuery, AccountingSettingsResponse, GetAccountingSettingsHandler>();
        services.AddCommandHandler<UpdateAccountingSettingsCommand, string, UpdateAccountingSettingsHandler>();

        services.AddCommandHandler<CreateAccountCommand, Guid, CreateAccountHandler>();
        services.AddCommandHandler<UpdateAccountCommand, string, UpdateAccountHandler>();
        services.AddCommandHandler<SetAccountActiveCommand, Done, SetAccountActiveHandler>();
        services.AddCommandHandler<DeleteAccountCommand, Done, DeleteAccountHandler>();
        services.AddCommandHandler<ImportChartOfAccountsCommand, ChartImportedResponse, ImportChartOfAccountsHandler>();
        services.AddCommandHandler<ApplyDefaultChartCommand, ChartImportedResponse, ApplyDefaultChartHandler>();
        services.AddQueryHandler<GetAccountQuery, AccountResponse, GetAccountHandler>();
        services.AddQueryHandler<ListAccountsQuery, PagedResult<AccountSummaryResponse>, ListAccountsHandler>();
        services.AddQueryHandler<GetAccountTreeQuery, IReadOnlyList<AccountSummaryResponse>, GetAccountTreeHandler>();

        services.AddQueryHandler<ListAccountMappingsQuery, IReadOnlyList<AccountMappingResponse>, ListAccountMappingsHandler>();
        services.AddCommandHandler<SetAccountMappingCommand, string, SetAccountMappingHandler>();
        services.AddCommandHandler<RemoveAccountMappingCommand, Done, RemoveAccountMappingHandler>();

        services.AddCommandHandler<CreateFiscalYearCommand, Guid, CreateFiscalYearHandler>();
        services.AddQueryHandler<ListFiscalYearsQuery, IReadOnlyList<FiscalYearResponse>, ListFiscalYearsHandler>();
        services.AddQueryHandler<GetFiscalYearQuery, FiscalYearResponse, GetFiscalYearHandler>();
        services.AddCommandHandler<CloseFiscalPeriodCommand, FiscalPeriodClosedResponse, CloseFiscalPeriodHandler>();
        services.AddCommandHandler<ReopenFiscalPeriodCommand, Done, ReopenFiscalPeriodHandler>();
        services.AddCommandHandler<CloseFiscalYearCommand, FiscalYearClosedResponse, CloseFiscalYearHandler>();
        services.AddCommandHandler<ReopenFiscalYearCommand, string, ReopenFiscalYearHandler>();

        services.AddCommandHandler<CreateJournalCommand, Guid, CreateJournalHandler>();
        services.AddCommandHandler<CreateOpeningJournalCommand, Guid, CreateOpeningJournalHandler>();
        services.AddCommandHandler<UpdateJournalCommand, string, UpdateJournalHandler>();
        services.AddCommandHandler<SubmitJournalCommand, string, SubmitJournalHandler>();
        services.AddCommandHandler<PostJournalCommand, JournalPostedResponse, PostJournalHandler>();
        services.AddCommandHandler<ApproveJournalCommand, JournalPostedResponse, ApproveJournalHandler>();
        services.AddCommandHandler<RejectJournalCommand, string, RejectJournalHandler>();
        services.AddCommandHandler<CancelJournalCommand, string, CancelJournalHandler>();
        services.AddCommandHandler<ReverseJournalCommand, Guid, ReverseJournalHandler>();
        services.AddQueryHandler<GetJournalQuery, JournalResponse, GetJournalHandler>();
        services.AddQueryHandler<ListJournalsQuery, PagedResult<JournalSummaryResponse>, ListJournalsHandler>();

        services.AddQueryHandler<GetTrialBalanceQuery, TrialBalanceResponse, GetTrialBalanceHandler>();
        services.AddQueryHandler<GetAccountStatementQuery, AccountStatementResponse, GetAccountStatementHandler>();
        services.AddQueryHandler<ListGeneralLedgerQuery, PagedResult<GeneralLedgerLineResponse>, ListGeneralLedgerHandler>();
        services.AddQueryHandler<GetBalanceSheetQuery, BalanceSheetResponse, GetBalanceSheetHandler>();
        services.AddQueryHandler<GetIncomeStatementQuery, IncomeStatementResponse, GetIncomeStatementHandler>();

        services.AddScoped<AccountingReferences>();
        services.AddScoped<IdempotentRequests>();
        services.AddScoped<LedgerPosting>();
        services.AddScoped<JournalLineRules>();
        services.AddScoped<JournalPosting>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountMappingRepository, AccountMappingRepository>();
        services.AddScoped<IAccountingSettingsRepository, AccountingSettingsRepository>();
        services.AddScoped<IFiscalYearRepository, FiscalYearRepository>();
        services.AddScoped<IJournalRepository, JournalRepository>();
        services.AddScoped<IAccountingLocks, AccountingLocks>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<IAccountReadStore, AccountReadStore>();
        services.AddScoped<IFiscalYearReadStore, FiscalYearReadStore>();
        services.AddScoped<IJournalReadStore, JournalReadStore>();
        services.AddScoped<ILedgerReadStore, LedgerReadStore>();

        services.AddPermissions("Accounting", AccountingPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder app) =>
        app.MapAccountingSettingsEndpoints()
            .MapAccountEndpoints()
            .MapAccountMappingEndpoints()
            .MapFiscalYearEndpoints()
            .MapJournalEndpoints()
            .MapFinancialReportEndpoints();
}
