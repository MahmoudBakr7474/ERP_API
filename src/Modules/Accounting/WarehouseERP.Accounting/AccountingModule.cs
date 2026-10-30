using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Accounting.Application;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Contracts.Cutover;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Features.Cutover;
using WarehouseERP.Accounting.Application.Features.Cutover.ActivateAccounting;
using WarehouseERP.Accounting.Application.Features.Cutover.GetCutoverReadiness;
using WarehouseERP.Accounting.Application.Features.SourcePostings;
using WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;
using WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceSyncStatus;
using WarehouseERP.Accounting.Application.Features.SourcePostings.ListSourcePostings;
using WarehouseERP.Accounting.Application.Features.SourcePostings.RetrySourcePosting;
using WarehouseERP.Accounting.Application.Features.SourcePostings.RunSourceSync;
using WarehouseERP.Accounting.Infrastructure;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;
using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Application.Features.Allocations.AllocateOpenItems;
using WarehouseERP.Accounting.Application.Features.Allocations.AutoAllocate;
using WarehouseERP.Accounting.Application.Features.Allocations.RemoveAllocation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.CancelBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.CompleteBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.GetBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.ListBankReconciliations;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.StartBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.TickBankReconciliationLines;
using WarehouseERP.Accounting.Application.Features.CashAccounts.CreateCashAccount;
using WarehouseERP.Accounting.Application.Features.CashAccounts.GetCashAccount;
using WarehouseERP.Accounting.Application.Features.CashAccounts.ListCashAccounts;
using WarehouseERP.Accounting.Application.Features.CashAccounts.SetCashAccountActive;
using WarehouseERP.Accounting.Application.Features.CashAccounts.UpdateCashAccount;
using WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;
using WarehouseERP.Accounting.Application.Features.Reports.GetAging;
using WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;
using WarehouseERP.Accounting.Application.Features.Vouchers;
using WarehouseERP.Accounting.Application.Features.Vouchers.CreateVoucher;
using WarehouseERP.Accounting.Application.Features.Vouchers.UpdateVoucher;
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

// The general ledger, subledgers and treasury (docs/modules/Accounting and ADR 0020): the chart of accounts and its
// posting-role mappings, fiscal years and periods, manual, opening-balance, reversal, voucher and year-end closing
// journals, customers' and suppliers' open items and allocations, cashboxes, banks and their reconciliation, and the reports
// computed from posted lines; and, once the cutover is activated, the journals of operational documents (Inventory's,
// Purchasing's, Sales' and POS's read-only accounting sources), journaled by SourcePostingWorker and on demand, with their
// exception queue and the source-to-ledger reconciliation. The company and branches come from Organization, display names
// from Identity, customers from Sales and suppliers from Purchasing; Sales reads receivable balances through
// ICustomerReceivables. The host calls AddAccountingModule and MapAccountingEndpoints; ITransactionRunner comes from
// Persistence.
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

        services.AddCommandHandler<CreateVoucherCommand, Guid, CreateVoucherHandler>();
        services.AddCommandHandler<UpdateVoucherCommand, string, UpdateVoucherHandler>();

        services.AddQueryHandler<ListOpenItemsQuery, PagedResult<OpenItemResponse>, ListOpenItemsHandler>();
        services.AddCommandHandler<AllocateOpenItemsCommand, Guid, AllocateOpenItemsHandler>();
        services.AddCommandHandler<RemoveAllocationCommand, Done, RemoveAllocationHandler>();
        services.AddCommandHandler<AutoAllocateCommand, AutoAllocatedResponse, AutoAllocateHandler>();

        services.AddCommandHandler<CreateCashAccountCommand, Guid, CreateCashAccountHandler>();
        services.AddCommandHandler<UpdateCashAccountCommand, string, UpdateCashAccountHandler>();
        services.AddCommandHandler<SetCashAccountActiveCommand, Done, SetCashAccountActiveHandler>();
        services.AddQueryHandler<GetCashAccountQuery, CashAccountResponse, GetCashAccountHandler>();
        services.AddQueryHandler<ListCashAccountsQuery, IReadOnlyList<CashAccountResponse>, ListCashAccountsHandler>();

        services.AddCommandHandler<StartBankReconciliationCommand, Guid, StartBankReconciliationHandler>();
        services.AddQueryHandler<GetBankReconciliationQuery, BankReconciliationResponse, GetBankReconciliationHandler>();
        services.AddQueryHandler<ListBankReconciliationsQuery, IReadOnlyList<BankReconciliationSummaryResponse>, ListBankReconciliationsHandler>();
        services.AddCommandHandler<TickBankReconciliationLinesCommand, string, TickBankReconciliationLinesHandler>();
        services.AddCommandHandler<CompleteBankReconciliationCommand, string, CompleteBankReconciliationHandler>();
        services.AddCommandHandler<CancelBankReconciliationCommand, string, CancelBankReconciliationHandler>();

        services.AddQueryHandler<GetCutoverReadinessQuery, CutoverReadinessResponse, GetCutoverReadinessHandler>();
        services.AddCommandHandler<ActivateAccountingCommand, string, ActivateAccountingHandler>();
        services.AddCommandHandler<RunSourceSyncCommand, SourceSyncResponse, RunSourceSyncHandler>();
        services.AddCommandHandler<RetrySourcePostingCommand, SourcePostingResponse, RetrySourcePostingHandler>();
        services.AddQueryHandler<ListSourcePostingsQuery, PagedResult<SourcePostingResponse>, ListSourcePostingsHandler>();
        services.AddQueryHandler<GetSourceSyncStatusQuery, SourceSyncStatusResponse, GetSourceSyncStatusHandler>();
        services.AddQueryHandler<GetSourceReconciliationQuery, SourceReconciliationResponse, GetSourceReconciliationHandler>();

        services.AddQueryHandler<GetAgingQuery, AgingResponse, GetAgingHandler>();
        services.AddQueryHandler<GetPartyStatementQuery, PartyStatementResponse, GetPartyStatementHandler>();
        services.AddQueryHandler<GetSubledgerReconciliationQuery, SubledgerReconciliationResponse, GetSubledgerReconciliationHandler>();
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
        services.AddScoped<PostingGuard>();
        services.AddScoped<CutoverChecks>();
        services.AddScoped<CutoverBoundary>();
        services.AddScoped<SourceDocuments>();
        services.AddScoped<SourceEntryBuilder>();
        services.AddScoped<SourcePoster>();
        services.AddScoped<SourceSync>();
        services.AddScoped<ISourcePostingRepository, SourcePostingRepository>();
        services.AddScoped<ISourcePostingReadStore, SourcePostingReadStore>();
        services.AddScoped<ICustomerReceivables, CustomerReceivables>();
        services.AddScoped<PartyDirectory>();
        services.AddScoped<VoucherBuilder>();
        services.AddScoped<IOpenItemRepository, OpenItemRepository>();
        services.AddScoped<IOpenItemReadStore, OpenItemReadStore>();
        services.AddScoped<ICashAccountRepository, CashAccountRepository>();
        services.AddScoped<ICashAccountReadStore, CashAccountReadStore>();
        services.AddScoped<IBankReconciliationRepository, BankReconciliationRepository>();
        services.AddScoped<IBankReconciliationReadStore, BankReconciliationReadStore>();
        services.AddScoped<ISubledgerReadStore, SubledgerReadStore>();
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

        services.AddOptions<AccountingIntegrationOptions>()
            .BindConfiguration(AccountingIntegrationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<SourcePostingWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<SourcePostingWorker>());

        services.AddPermissions("Accounting", AccountingPermissions.All);
        return services;
    }

    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder app) =>
        app.MapAccountingSettingsEndpoints()
            .MapAccountEndpoints()
            .MapAccountMappingEndpoints()
            .MapFiscalYearEndpoints()
            .MapJournalEndpoints()
            .MapVoucherEndpoints()
            .MapOpenItemEndpoints()
            .MapCashAccountEndpoints()
            .MapBankReconciliationEndpoints()
            .MapCutoverEndpoints()
            .MapSourcePostingEndpoints()
            .MapFinancialReportEndpoints();
}
