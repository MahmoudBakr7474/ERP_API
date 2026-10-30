using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration14_Accounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounting");

            migrationBuilder.CreateTable(
                name: "AccountingSettings",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    CutoverDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ManualJournalsRequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSettings", x => x.Id);
                    table.CheckConstraint("CK_AccountingSettings_Cutover", "DAY([CutoverDate]) = 1");
                    table.CheckConstraint("CK_AccountingSettings_Single", "[Id] = 1");
                    table.CheckConstraint("CK_AccountingSettings_StartMonth", "[FiscalYearStartMonth] BETWEEN 1 AND 12");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountingSettingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Section = table.Column<byte>(type: "tinyint", nullable: true),
                    IsPosting = table.Column<bool>(type: "bit", nullable: false),
                    AllowsManualPosting = table.Column<bool>(type: "bit", nullable: false),
                    PartyType = table.Column<byte>(type: "tinyint", nullable: true),
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
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_Level", "[Level] BETWEEN 1 AND 8");
                    table.CheckConstraint("CK_Accounts_Manual", "[AllowsManualPosting] = 0 OR [IsPosting] = 1");
                    table.CheckConstraint("CK_Accounts_Party", "[PartyType] IS NULL OR ([IsPosting] = 1 AND [PartyType] BETWEEN 1 AND 2)");
                    table.CheckConstraint("CK_Accounts_PostingSection", "[IsPosting] = 0 OR [Section] IS NOT NULL");
                    table.CheckConstraint("CK_Accounts_Section", "[Section] IS NULL OR [Section] BETWEEN 1 AND 11");
                    table.CheckConstraint("CK_Accounts_Type", "[Type] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "accounting",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "accounting",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Fingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AccountMappings",
                schema: "accounting",
                columns: table => new
                {
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountMappings", x => x.Role);
                    table.CheckConstraint("CK_AccountMappings_Role", "[Role] BETWEEN 1 AND 28");
                    table.ForeignKey(
                        name: "FK_AccountMappings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountMappingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "DocumentNumberSequences",
                schema: "accounting",
                columns: table => new
                {
                    JournalType = table.Column<byte>(type: "tinyint", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNumberSequences", x => new { x.JournalType, x.FiscalYearId });
                    table.CheckConstraint("CK_DocumentNumberSequences_Last", "[LastNumber] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                    table.CheckConstraint("CK_FiscalPeriods_Closed", "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL) OR ([Status] = 1 AND [ClosedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_FiscalPeriods_Dates", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_FiscalPeriods_Number", "[Number] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_FiscalPeriods_Status", "[Status] BETWEEN 1 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "FiscalYearEvents",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearEvents", x => x.Id);
                    table.CheckConstraint("CK_FiscalYearEvents_Action", "[Action] BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosingJournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                    table.CheckConstraint("CK_FiscalYears_Closed", "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL) OR ([Status] = 1 AND [ClosedAtUtc] IS NULL AND [ClosingJournalId] IS NULL)");
                    table.CheckConstraint("CK_FiscalYears_Dates", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_FiscalYears_Status", "[Status] BETWEEN 1 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    JournalType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    LinesFixed = table.Column<bool>(type: "bit", nullable: false),
                    ReversalOfJournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalDebit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.CheckConstraint("CK_JournalEntries_Balanced", "[TotalDebit] = [TotalCredit] AND [TotalDebit] > 0");
                    table.CheckConstraint("CK_JournalEntries_Posted", "([Status] = 3 AND [Number] IS NOT NULL AND [FiscalYearId] IS NOT NULL AND [FiscalPeriodId] IS NOT NULL AND [SequenceNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL AND [PostedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_JournalEntries_Reversal", "([JournalType] = 3 AND [ReversalOfJournalId] IS NOT NULL) OR ([JournalType] <> 3 AND [ReversalOfJournalId] IS NULL)");
                    table.CheckConstraint("CK_JournalEntries_Status", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_JournalEntries_Type", "[JournalType] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_JournalEntries_FiscalPeriods_FiscalPeriodId",
                        column: x => x.FiscalPeriodId,
                        principalSchema: "accounting",
                        principalTable: "FiscalPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalSchema: "accounting",
                        principalTable: "FiscalYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_JournalEntries_ReversalOfJournalId",
                        column: x => x.ReversalOfJournalId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEvents",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEvents", x => x.Id);
                    table.CheckConstraint("CK_JournalEvents_Action", "[Action] BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "FK_JournalEvents_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.CheckConstraint("CK_JournalLines_OneSide", "[Debit] >= 0 AND [Credit] >= 0 AND (([Debit] > 0 AND [Credit] = 0) OR ([Debit] = 0 AND [Credit] > 0))");
                    table.ForeignKey(
                        name: "FK_JournalLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "accounting",
                table: "AccountingSettings",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "CutoverDate", "FiscalYearStartMonth", "ManualJournalsRequireApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { 1, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateOnly(2027, 1, 1), 1, true, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AccountMappings_AccountId",
                schema: "accounting",
                table: "AccountMappings",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentId",
                schema: "accounting",
                table: "Accounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_Accounts_Code",
                schema: "accounting",
                table: "Accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_FiscalYearId",
                schema: "accounting",
                table: "DocumentNumberSequences",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_FiscalYearId_Number",
                schema: "accounting",
                table: "FiscalPeriods",
                columns: new[] { "FiscalYearId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FiscalPeriods_StartDate",
                schema: "accounting",
                table: "FiscalPeriods",
                column: "StartDate",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "EndDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearEvents_FiscalYearId",
                schema: "accounting",
                table: "FiscalYearEvents",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_ClosingJournalId",
                schema: "accounting",
                table: "FiscalYears",
                column: "ClosingJournalId");

            migrationBuilder.CreateIndex(
                name: "UX_FiscalYears_StartDate",
                schema: "accounting",
                table: "FiscalYears",
                column: "StartDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "accounting",
                table: "IdempotencyRecords",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreatedAtUtc",
                schema: "accounting",
                table: "JournalEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_DraftNumber",
                schema: "accounting",
                table: "JournalEntries",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FiscalPeriodId",
                schema: "accounting",
                table: "JournalEntries",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FiscalYearId",
                schema: "accounting",
                table: "JournalEntries",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_JournalType_FiscalYearId_SequenceNumber",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "JournalType", "FiscalYearId", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Number",
                schema: "accounting",
                table: "JournalEntries",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Status_PostingDate",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "Status", "PostingDate" })
                .Annotation("SqlServer:Include", new[] { "JournalType" });

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_Reversal",
                schema: "accounting",
                table: "JournalEntries",
                column: "ReversalOfJournalId",
                unique: true,
                filter: "[ReversalOfJournalId] IS NOT NULL AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEvents_JournalEntryId",
                schema: "accounting",
                table: "JournalEvents",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId",
                schema: "accounting",
                table: "JournalLines",
                column: "AccountId")
                .Annotation("SqlServer:Include", new[] { "JournalEntryId", "BranchId", "Debit", "Credit" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_BranchId",
                schema: "accounting",
                table: "JournalLines",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalEntryId_LineNumber",
                schema: "accounting",
                table: "JournalLines",
                columns: new[] { "JournalEntryId", "LineNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentNumberSequences_FiscalYears_FiscalYearId",
                schema: "accounting",
                table: "DocumentNumberSequences",
                column: "FiscalYearId",
                principalSchema: "accounting",
                principalTable: "FiscalYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_FiscalYears_FiscalYearId",
                schema: "accounting",
                table: "FiscalPeriods",
                column: "FiscalYearId",
                principalSchema: "accounting",
                principalTable: "FiscalYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYearEvents_FiscalYears_FiscalYearId",
                schema: "accounting",
                table: "FiscalYearEvents",
                column: "FiscalYearId",
                principalSchema: "accounting",
                principalTable: "FiscalYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_JournalEntries_ClosingJournalId",
                schema: "accounting",
                table: "FiscalYears",
                column: "ClosingJournalId",
                principalSchema: "accounting",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_FiscalYears_FiscalYearId",
                schema: "accounting",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_FiscalYears_FiscalYearId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropTable(
                name: "AccountingSettings",
                schema: "accounting")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountingSettingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "AccountMappings",
                schema: "accounting")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountMappingsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "DocumentNumberSequences",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FiscalYearEvents",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalEvents",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalLines",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "accounting")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AccountsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "FiscalYears",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalEntries",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FiscalPeriods",
                schema: "accounting");
        }
    }
}
