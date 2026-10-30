using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration15_AccountingSubledgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                schema: "accounting",
                table: "JournalLines",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartyId",
                schema: "accounting",
                table: "JournalLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PartyType",
                schema: "accounting",
                table: "JournalLines",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                schema: "accounting",
                table: "JournalEntries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartyId",
                schema: "accounting",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PartyType",
                schema: "accounting",
                table: "JournalEntries",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentMethod",
                schema: "accounting",
                table: "JournalEntries",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "accounting",
                table: "JournalEntries",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherAmount",
                schema: "accounting",
                table: "JournalEntries",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                schema: "accounting",
                table: "JournalEntries",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VouchersRequireApproval",
                schema: "accounting",
                table: "AccountingSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CashAccounts",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AllowsOverdraft = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccounts", x => x.Id);
                    table.CheckConstraint("CK_CashAccounts_Cashbox", "[Kind] = 2 OR ([BankName] IS NULL AND [BankAccountNumber] IS NULL AND [AllowsOverdraft] = 0)");
                    table.CheckConstraint("CK_CashAccounts_Kind", "[Kind] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_CashAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccounts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CashAccountsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "OpenItems",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyType = table.Column<byte>(type: "tinyint", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalType = table.Column<byte>(type: "tinyint", nullable: false),
                    DocumentNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    OpenAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenItems", x => x.Id);
                    table.CheckConstraint("CK_OpenItems_Open", "([Amount] > 0 AND [OpenAmount] BETWEEN 0 AND [Amount]) OR ([Amount] < 0 AND [OpenAmount] BETWEEN [Amount] AND 0)");
                    table.CheckConstraint("CK_OpenItems_PartyType", "[PartyType] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_OpenItems_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpenItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpenItems_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpenItems_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalSchema: "accounting",
                        principalTable: "JournalLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankReconciliations",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StatementBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OpeningClearedBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliations", x => x.Id);
                    table.CheckConstraint("CK_BankReconciliations_Completed", "([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [OpeningClearedBalance] IS NOT NULL) OR ([Status] <> 2 AND [CompletedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_BankReconciliations_Status", "[Status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_BankReconciliations_CashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalSchema: "accounting",
                        principalTable: "CashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Allocations",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyType = table.Column<byte>(type: "tinyint", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebitItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AllocatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                    table.CheckConstraint("CK_Allocations_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Allocations_Items", "[DebitItemId] <> [CreditItemId]");
                    table.CheckConstraint("CK_Allocations_Removed", "([IsActive] = 1 AND [RemovedAtUtc] IS NULL) OR ([IsActive] = 0 AND [RemovedAtUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Allocations_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Allocations_OpenItems_CreditItemId",
                        column: x => x.CreditItemId,
                        principalSchema: "accounting",
                        principalTable: "OpenItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Allocations_OpenItems_DebitItemId",
                        column: x => x.DebitItemId,
                        principalSchema: "accounting",
                        principalTable: "OpenItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoucherAllocations",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherAllocations", x => x.Id);
                    table.CheckConstraint("CK_VoucherAllocations_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_VoucherAllocations_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherAllocations_OpenItems_OpenItemId",
                        column: x => x.OpenItemId,
                        principalSchema: "accounting",
                        principalTable: "OpenItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankReconciliationLines",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankReconciliationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankReconciliationLines_BankReconciliations_BankReconciliationId",
                        column: x => x.BankReconciliationId,
                        principalSchema: "accounting",
                        principalTable: "BankReconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankReconciliationLines_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalSchema: "accounting",
                        principalTable: "JournalLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_PartyType_PartyId",
                schema: "accounting",
                table: "JournalLines",
                columns: new[] { "PartyType", "PartyId" },
                filter: "[PartyId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalLines_Party",
                schema: "accounting",
                table: "JournalLines",
                sql: "([PartyType] IS NULL AND [PartyId] IS NULL AND [DueDate] IS NULL) OR ([PartyType] BETWEEN 1 AND 2 AND [PartyId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                column: "CounterCashAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_BillNumber",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "PartyId", "Reference" },
                unique: true,
                filter: "[JournalType] = 12 AND [Status] <> 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Party",
                schema: "accounting",
                table: "JournalEntries",
                sql: "([PartyType] IS NULL AND [PartyId] IS NULL) OR ([PartyType] IS NOT NULL AND [PartyId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries",
                sql: "[JournalType] BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Voucher",
                schema: "accounting",
                table: "JournalEntries",
                sql: "[JournalType] BETWEEN 5 AND 12 OR ([CashAccountId] IS NULL AND [CounterCashAccountId] IS NULL AND [PartyId] IS NULL AND [PaymentMethod] IS NULL AND [VoucherAmount] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_CreditItemId",
                schema: "accounting",
                table: "Allocations",
                column: "CreditItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_DebitItemId",
                schema: "accounting",
                table: "Allocations",
                column: "DebitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_JournalEntryId",
                schema: "accounting",
                table: "Allocations",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_PartyType_PartyId",
                schema: "accounting",
                table: "Allocations",
                columns: new[] { "PartyType", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationLines_BankReconciliationId",
                schema: "accounting",
                table: "BankReconciliationLines",
                column: "BankReconciliationId");

            migrationBuilder.CreateIndex(
                name: "UX_BankReconciliationLines_Line",
                schema: "accounting",
                table: "BankReconciliationLines",
                column: "JournalLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliations_CashAccountId_StatementDate",
                schema: "accounting",
                table: "BankReconciliations",
                columns: new[] { "CashAccountId", "StatementDate" });

            migrationBuilder.CreateIndex(
                name: "UX_BankReconciliations_Draft",
                schema: "accounting",
                table: "BankReconciliations",
                column: "CashAccountId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_BranchId",
                schema: "accounting",
                table: "CashAccounts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "UX_CashAccounts_Account",
                schema: "accounting",
                table: "CashAccounts",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CashAccounts_Code",
                schema: "accounting",
                table: "CashAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenItems_AccountId",
                schema: "accounting",
                table: "OpenItems",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenItems_BranchId",
                schema: "accounting",
                table: "OpenItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenItems_JournalEntryId",
                schema: "accounting",
                table: "OpenItems",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenItems_PartyType_PartyId_DueDate",
                schema: "accounting",
                table: "OpenItems",
                columns: new[] { "PartyType", "PartyId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "UX_OpenItems_Line",
                schema: "accounting",
                table: "OpenItems",
                column: "JournalLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherAllocations_JournalEntryId_OpenItemId",
                schema: "accounting",
                table: "VoucherAllocations",
                columns: new[] { "JournalEntryId", "OpenItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherAllocations_OpenItemId",
                schema: "accounting",
                table: "VoucherAllocations",
                column: "OpenItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_CashAccounts_CashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                column: "CashAccountId",
                principalSchema: "accounting",
                principalTable: "CashAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_CashAccounts_CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries",
                column: "CounterCashAccountId",
                principalSchema: "accounting",
                principalTable: "CashAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_CashAccounts_CashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_CashAccounts_CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropTable(
                name: "Allocations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "BankReconciliationLines",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "VoucherAllocations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "BankReconciliations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "OpenItems",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "CashAccounts",
                schema: "accounting")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CashAccountsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_PartyType_PartyId",
                schema: "accounting",
                table: "JournalLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalLines_Party",
                schema: "accounting",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_CashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "UX_JournalEntries_BillNumber",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Party",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Voucher",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "accounting",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "PartyId",
                schema: "accounting",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "PartyType",
                schema: "accounting",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "CashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "CounterCashAccountId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PartyId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PartyType",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VoucherAmount",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VouchersRequireApproval",
                schema: "accounting",
                table: "AccountingSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries",
                sql: "[JournalType] BETWEEN 1 AND 4");
        }
    }
}
