using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration16_AccountingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                schema: "accounting",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "SourceType",
                schema: "accounting",
                table: "JournalEntries",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceivesPosCash",
                schema: "accounting",
                table: "CashAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAtUtc",
                schema: "accounting",
                table: "AccountingSettings",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivatedByUserId",
                schema: "accounting",
                table: "AccountingSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CutoverStartUtc",
                schema: "accounting",
                table: "AccountingSettings",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SourceCursors",
                schema: "accounting",
                columns: table => new
                {
                    Module = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ScannedToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceCursors", x => x.Module);
                });

            migrationBuilder.CreateTable(
                name: "SourcePostings",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    JournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StockValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    PostedStockValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    VarianceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    DiscoveredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePostings", x => x.Id);
                    table.CheckConstraint("CK_SourcePostings_Next", "([Status] IN (1, 3) AND [NextAttemptAtUtc] IS NOT NULL) OR ([Status] IN (2, 4) AND [NextAttemptAtUtc] IS NULL)");
                    table.CheckConstraint("CK_SourcePostings_Posted", "([Status] = 2 AND [JournalId] IS NOT NULL AND [JournalNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 2 AND [JournalId] IS NULL AND [JournalNumber] IS NULL)");
                    table.CheckConstraint("CK_SourcePostings_Status", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_SourcePostings_Type", "[SourceType] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_SourcePostings_JournalEntries_JournalId",
                        column: x => x.JournalId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_Source",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "SourceType", "SourceId" },
                unique: true,
                filter: "[SourceId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Source",
                schema: "accounting",
                table: "JournalEntries",
                sql: "([JournalType] BETWEEN 13 AND 16 AND [SourceType] IS NOT NULL AND [SourceId] IS NOT NULL) OR ([JournalType] NOT BETWEEN 13 AND 16 AND [SourceType] IS NULL AND [SourceId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries",
                sql: "[JournalType] BETWEEN 1 AND 16");

            migrationBuilder.CreateIndex(
                name: "UX_CashAccounts_PosCash",
                schema: "accounting",
                table: "CashAccounts",
                column: "BranchId",
                unique: true,
                filter: "[ReceivesPosCash] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CashAccounts_PosCash",
                schema: "accounting",
                table: "CashAccounts",
                sql: "[ReceivesPosCash] = 0 OR [Kind] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePostings_JournalId",
                schema: "accounting",
                table: "SourcePostings",
                column: "JournalId",
                unique: true,
                filter: "[JournalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePostings_OccurredAtUtc",
                schema: "accounting",
                table: "SourcePostings",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePostings_Status_NextAttemptAtUtc",
                schema: "accounting",
                table: "SourcePostings",
                columns: new[] { "Status", "NextAttemptAtUtc" })
                .Annotation("SqlServer:Include", new[] { "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SourcePostings_Source",
                schema: "accounting",
                table: "SourcePostings",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceCursors",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "SourcePostings",
                schema: "accounting");

            migrationBuilder.DropIndex(
                name: "UX_JournalEntries_Source",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Source",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "UX_CashAccounts_PosCash",
                schema: "accounting",
                table: "CashAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CashAccounts_PosCash",
                schema: "accounting",
                table: "CashAccounts");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "accounting",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReceivesPosCash",
                schema: "accounting",
                table: "CashAccounts");

            migrationBuilder.DropColumn(
                name: "ActivatedAtUtc",
                schema: "accounting",
                table: "AccountingSettings");

            migrationBuilder.DropColumn(
                name: "ActivatedByUserId",
                schema: "accounting",
                table: "AccountingSettings");

            migrationBuilder.DropColumn(
                name: "CutoverStartUtc",
                schema: "accounting",
                table: "AccountingSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JournalEntries_Type",
                schema: "accounting",
                table: "JournalEntries",
                sql: "[JournalType] BETWEEN 1 AND 12");
        }
    }
}
