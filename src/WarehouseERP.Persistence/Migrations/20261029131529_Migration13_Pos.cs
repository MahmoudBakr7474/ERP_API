using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration13_Pos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pos");

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "pos",
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
                name: "PosSettings",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MaxLineDiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    ReturnWindowDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSettings", x => x.Id);
                    table.CheckConstraint("CK_PosSettings_MaxDiscount", "[MaxLineDiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_PosSettings_ReturnWindow", "[ReturnWindowDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_PosSettings_Single", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "PosTerminals",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellingLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptsCash = table.Column<bool>(type: "bit", nullable: false),
                    AcceptsCard = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptHeader = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptFooter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PosTerminals", x => x.Id);
                    table.CheckConstraint("CK_PosTerminals_PaymentMethod", "[AcceptsCash] = 1 OR [AcceptsCard] = 1");
                    table.ForeignKey(
                        name: "FK_PosTerminals_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosTerminals_WarehouseLocations_SellingLocationId",
                        column: x => x.SellingLocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosTerminals_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PosTerminalsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "PosNumberSequences",
                schema: "pos",
                columns: table => new
                {
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosNumberSequences", x => new { x.DocumentType, x.TerminalId, x.FiscalYear });
                    table.CheckConstraint("CK_PosNumberSequences_LastNumber", "[LastNumber] > 0");
                    table.CheckConstraint("CK_PosNumberSequences_Type", "[DocumentType] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_PosNumberSequences_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosShifts",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    OpeningFloat = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashSalesAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashRefundsAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashInAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashOutAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CardSalesAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CardRefundsAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SalesAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RefundsAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SalesCount = table.Column<int>(type: "int", nullable: false),
                    ReturnsCount = table.Column<int>(type: "int", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsForceClosed = table.Column<bool>(type: "bit", nullable: false),
                    CountedCash = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ExpectedCashAtClose = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    CashDifference = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ClosingNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosShifts", x => x.Id);
                    table.CheckConstraint("CK_PosShifts_Amounts", "[OpeningFloat] >= 0 AND [CashSalesAmount] >= 0 AND [CashRefundsAmount] >= 0 AND [CashInAmount] >= 0 AND [CashOutAmount] >= 0 AND [CardSalesAmount] >= 0 AND [CardRefundsAmount] >= 0 AND [SalesAmount] >= 0 AND [RefundsAmount] >= 0 AND [SalesCount] >= 0 AND [ReturnsCount] >= 0");
                    table.CheckConstraint("CK_PosShifts_Closed", "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL AND [CountedCash] IS NOT NULL AND [ExpectedCashAtClose] IS NOT NULL AND [CashDifference] = [CountedCash] - [ExpectedCashAtClose]) OR ([Status] = 1 AND [ClosedAtUtc] IS NULL AND [CountedCash] IS NULL AND [CashDifference] IS NULL)");
                    table.CheckConstraint("CK_PosShifts_Drawer", "[OpeningFloat] + [CashSalesAmount] - [CashRefundsAmount] + [CashInAmount] - [CashOutAmount] >= 0");
                    table.CheckConstraint("CK_PosShifts_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PosShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosShifts_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosShifts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosCashMovements",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosCashMovements", x => x.Id);
                    table.CheckConstraint("CK_PosCashMovements_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_PosCashMovements_Type", "[MovementType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PosCashMovements_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosSales",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CustomerTaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    CurrencyDecimals = table.Column<byte>(type: "tinyint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashTenderedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashReceivedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CardAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RefundedCashAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RefundedCardAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ReturnCount = table.Column<int>(type: "int", nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PrintCount = table.Column<int>(type: "int", nullable: false),
                    LastPrintedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastPrintedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSales", x => x.Id);
                    table.CheckConstraint("CK_PosSales_Amounts", "[GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] = [GrossAmount] - [DiscountAmount] AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount] AND [CostAmount] >= 0");
                    table.CheckConstraint("CK_PosSales_Payments", "[CashTenderedAmount] >= 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [CashTenderedAmount] AND [CashReceivedAmount] = [CashTenderedAmount] - [ChangeAmount] AND [CardAmount] >= 0 AND [CardAmount] <= [TotalAmount] AND [CashReceivedAmount] + [CardAmount] = [TotalAmount]");
                    table.CheckConstraint("CK_PosSales_Posted", "[StockDocumentId] IS NOT NULL AND [StockDocumentNumber] IS NOT NULL");
                    table.CheckConstraint("CK_PosSales_PrintCount", "[PrintCount] >= 1");
                    table.CheckConstraint("CK_PosSales_Refunds", "[RefundedCashAmount] >= 0 AND [RefundedCashAmount] <= [CashReceivedAmount] AND [RefundedCardAmount] >= 0 AND [RefundedCardAmount] <= [CardAmount] AND [ReturnCount] >= 0");
                    table.ForeignKey(
                        name: "FK_PosSales_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosShiftEvents",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosShiftEvents", x => x.Id);
                    table.CheckConstraint("CK_PosShiftEvents_Action", "[Action] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_PosShiftEvents_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosCardIncidents",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CardAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    References = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosCardIncidents", x => x.Id);
                    table.CheckConstraint("CK_PosCardIncidents_Amount", "[CardAmount] > 0 AND [Attempts] >= 1");
                    table.CheckConstraint("CK_PosCardIncidents_Resolved", "([Status] = 1 AND [ResolvedAtUtc] IS NULL) OR ([Status] = 2 AND [SaleId] IS NOT NULL AND [ResolvedAtUtc] IS NOT NULL) OR ([Status] = 3 AND [ResolvedAtUtc] IS NOT NULL AND [ResolutionNote] IS NOT NULL)");
                    table.CheckConstraint("CK_PosCardIncidents_Status", "[Status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_PosCardIncidents_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosCardIncidents_PosSales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosCardIncidents_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosCardIncidents_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosCardIncidents_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosReturns",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CashRefundAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CardRefundAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PrintCount = table.Column<int>(type: "int", nullable: false),
                    LastPrintedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastPrintedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosReturns", x => x.Id);
                    table.CheckConstraint("CK_PosReturns_Amounts", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount] AND [CostAmount] >= 0 AND [CashRefundAmount] >= 0 AND [CardRefundAmount] >= 0 AND [CashRefundAmount] + [CardRefundAmount] = [TotalAmount]");
                    table.CheckConstraint("CK_PosReturns_Posted", "[StockDocumentId] IS NOT NULL AND [StockDocumentNumber] IS NOT NULL");
                    table.CheckConstraint("CK_PosReturns_PrintCount", "[PrintCount] >= 1");
                    table.ForeignKey(
                        name: "FK_PosReturns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_PosSales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosSaleLines",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    StockLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundedNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RefundedTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSaleLines", x => x.Id);
                    table.CheckConstraint("CK_PosSaleLines_Amounts", "[GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] = [GrossAmount] - [DiscountAmount] AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]");
                    table.CheckConstraint("CK_PosSaleLines_Price", "[UnitPrice] > 0 AND [DiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_PosSaleLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_PosSaleLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity] AND [RefundedNetAmount] >= 0 AND [RefundedNetAmount] <= [NetAmount] AND [RefundedTaxAmount] >= 0 AND [RefundedTaxAmount] <= [TaxAmount]");
                    table.CheckConstraint("CK_PosSaleLines_Tax", "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
                    table.ForeignKey(
                        name: "FK_PosSaleLines_PosSales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_StockDocumentLines_StockLineId",
                        column: x => x.StockLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosSalePayments",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TenderedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardLast4 = table.Column<string>(type: "char(4)", unicode: false, fixedLength: true, maxLength: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSalePayments", x => x.Id);
                    table.CheckConstraint("CK_PosSalePayments_Amounts", "[Amount] >= 0 AND [ChangeAmount] >= 0 AND [Amount] = [TenderedAmount] - [ChangeAmount]");
                    table.CheckConstraint("CK_PosSalePayments_CardLast4", "[CardLast4] IS NULL OR [CardLast4] LIKE '[0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_PosSalePayments_Method", "([Method] = 1 AND [Status] = 1 AND [CardLast4] IS NULL) OR ([Method] = 2 AND [Status] = 2 AND [Reference] IS NOT NULL AND [ChangeAmount] = 0 AND [Amount] > 0)");
                    table.ForeignKey(
                        name: "FK_PosSalePayments_PosSales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosRefunds",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardLast4 = table.Column<string>(type: "char(4)", unicode: false, fixedLength: true, maxLength: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosRefunds", x => x.Id);
                    table.CheckConstraint("CK_PosRefunds_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_PosRefunds_Method", "([Method] = 1 AND [Status] = 1 AND [CardLast4] IS NULL) OR ([Method] = 2 AND [Status] = 2 AND [Reference] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PosRefunds_PosReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "pos",
                        principalTable: "PosReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosReturnLines",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    SaleLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Condition = table.Column<byte>(type: "tinyint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SoldStockLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosReturnLines", x => x.Id);
                    table.CheckConstraint("CK_PosReturnLines_Amounts", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]");
                    table.CheckConstraint("CK_PosReturnLines_Condition", "[Condition] = 1");
                    table.CheckConstraint("CK_PosReturnLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.ForeignKey(
                        name: "FK_PosReturnLines_PosReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "pos",
                        principalTable: "PosReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_PosSaleLines_SaleLineId",
                        column: x => x.SaleLineId,
                        principalSchema: "pos",
                        principalTable: "PosSaleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_StockDocumentLines_SoldStockLineId",
                        column: x => x.SoldStockLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_StockDocumentLines_StockLineId",
                        column: x => x.StockLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosReturnLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "pos",
                table: "PosSettings",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "MaxLineDiscountPercent", "ReturnWindowDays", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { 1, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), null, 10m, 14, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "pos",
                table: "IdempotencyRecords",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCardIncidents_BranchId",
                schema: "pos",
                table: "PosCardIncidents",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCardIncidents_SaleId",
                schema: "pos",
                table: "PosCardIncidents",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCardIncidents_ShiftId",
                schema: "pos",
                table: "PosCardIncidents",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCardIncidents_TerminalId",
                schema: "pos",
                table: "PosCardIncidents",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCardIncidents_WarehouseId_Status",
                schema: "pos",
                table: "PosCardIncidents",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_PosCardIncidents_Key",
                schema: "pos",
                table: "PosCardIncidents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosCashMovements_ShiftId",
                schema: "pos",
                table: "PosCashMovements",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosNumberSequences_TerminalId",
                schema: "pos",
                table: "PosNumberSequences",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosRefunds_ReturnId_LineNumber",
                schema: "pos",
                table: "PosRefunds",
                columns: new[] { "ReturnId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_ProductId",
                schema: "pos",
                table: "PosReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_ProductUnitId",
                schema: "pos",
                table: "PosReturnLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_ReturnId_LineNumber",
                schema: "pos",
                table: "PosReturnLines",
                columns: new[] { "ReturnId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_SaleLineId",
                schema: "pos",
                table: "PosReturnLines",
                column: "SaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_SoldStockLineId",
                schema: "pos",
                table: "PosReturnLines",
                column: "SoldStockLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_StockLineId",
                schema: "pos",
                table: "PosReturnLines",
                column: "StockLineId",
                unique: true,
                filter: "[StockLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturnLines_UnitId",
                schema: "pos",
                table: "PosReturnLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_BranchId",
                schema: "pos",
                table: "PosReturns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_LocationId",
                schema: "pos",
                table: "PosReturns",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_Number",
                schema: "pos",
                table: "PosReturns",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_SaleId",
                schema: "pos",
                table: "PosReturns",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_ShiftId",
                schema: "pos",
                table: "PosReturns",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_TerminalId_FiscalYear_SequenceNumber",
                schema: "pos",
                table: "PosReturns",
                columns: new[] { "TerminalId", "FiscalYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_WarehouseId_ReturnedAtUtc",
                schema: "pos",
                table: "PosReturns",
                columns: new[] { "WarehouseId", "ReturnedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PosReturns_StockDocument",
                schema: "pos",
                table: "PosReturns",
                column: "StockDocumentId",
                unique: true,
                filter: "[StockDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_ProductId",
                schema: "pos",
                table: "PosSaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_ProductUnitId",
                schema: "pos",
                table: "PosSaleLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_SaleId_LineNumber",
                schema: "pos",
                table: "PosSaleLines",
                columns: new[] { "SaleId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_StockLineId",
                schema: "pos",
                table: "PosSaleLines",
                column: "StockLineId",
                unique: true,
                filter: "[StockLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_UnitId",
                schema: "pos",
                table: "PosSaleLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSalePayments_SaleId_LineNumber",
                schema: "pos",
                table: "PosSalePayments",
                columns: new[] { "SaleId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_BranchId",
                schema: "pos",
                table: "PosSales",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_CustomerId",
                schema: "pos",
                table: "PosSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_LocationId",
                schema: "pos",
                table: "PosSales",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_Number",
                schema: "pos",
                table: "PosSales",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_ShiftId",
                schema: "pos",
                table: "PosSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_SoldAtUtc",
                schema: "pos",
                table: "PosSales",
                column: "SoldAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TerminalId_FiscalYear_SequenceNumber",
                schema: "pos",
                table: "PosSales",
                columns: new[] { "TerminalId", "FiscalYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_WarehouseId_SoldAtUtc",
                schema: "pos",
                table: "PosSales",
                columns: new[] { "WarehouseId", "SoldAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PosSales_StockDocument",
                schema: "pos",
                table: "PosSales",
                column: "StockDocumentId",
                unique: true,
                filter: "[StockDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosShiftEvents_ShiftId",
                schema: "pos",
                table: "PosShiftEvents",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_BranchId",
                schema: "pos",
                table: "PosShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_Number",
                schema: "pos",
                table: "PosShifts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_OpenedAtUtc",
                schema: "pos",
                table: "PosShifts",
                column: "OpenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_TerminalId_FiscalYear_SequenceNumber",
                schema: "pos",
                table: "PosShifts",
                columns: new[] { "TerminalId", "FiscalYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_WarehouseId_Status",
                schema: "pos",
                table: "PosShifts",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_PosShifts_OpenCashier",
                schema: "pos",
                table: "PosShifts",
                column: "CashierUserId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_PosShifts_OpenTerminal",
                schema: "pos",
                table: "PosShifts",
                column: "TerminalId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_BranchId",
                schema: "pos",
                table: "PosTerminals",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_SellingLocationId",
                schema: "pos",
                table: "PosTerminals",
                column: "SellingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_WarehouseId",
                schema: "pos",
                table: "PosTerminals",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UX_PosTerminals_Code",
                schema: "pos",
                table: "PosTerminals",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosCardIncidents",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosCashMovements",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosNumberSequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosRefunds",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosReturnLines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosSalePayments",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosSettings",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosShiftEvents",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosReturns",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosSaleLines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosSales",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosShifts",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosTerminals",
                schema: "pos")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PosTerminalsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");
        }
    }
}
