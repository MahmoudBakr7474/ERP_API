using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration04_Inventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "DocumentNumberSequences",
                schema: "inventory",
                columns: table => new
                {
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNumberSequences", x => new { x.DocumentType, x.BranchId, x.FiscalYear });
                    table.CheckConstraint("CK_DocumentNumberSequences_LastNumber", "[LastNumber] > 0");
                    table.ForeignKey(
                        name: "FK_DocumentNumberSequences_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypePolicies",
                schema: "inventory",
                columns: table => new
                {
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypePolicies", x => x.DocumentType);
                    table.CheckConstraint("CK_DocumentTypePolicies_Type", "[DocumentType] BETWEEN 1 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostStates",
                schema: "inventory",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AverageUnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostStates", x => x.ProductId);
                    table.CheckConstraint("CK_InventoryCostStates_AverageUnitCost", "[AverageUnitCost] >= 0");
                    table.CheckConstraint("CK_InventoryCostStates_TotalQuantity", "[TotalQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryCostStates_TotalValue", "[TotalValue] >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryCostStates_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductWarehouseStock",
                schema: "inventory",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandSellable = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWarehouseStock", x => new { x.ProductId, x.WarehouseId });
                    table.CheckConstraint("CK_ProductWarehouseStock_OnHandSellable", "[OnHandSellable] >= 0");
                    table.CheckConstraint("CK_ProductWarehouseStock_ReservedQuantity", "[ReservedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductWarehouseStock_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductWarehouseStock_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBalances",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalances", x => x.Id);
                    table.CheckConstraint("CK_StockBalances_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_StockBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalances_WarehouseLocations_WarehouseId_LocationId",
                        columns: x => new { x.WarehouseId, x.LocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockDocuments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ReversalOfDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdjustmentReason = table.Column<byte>(type: "tinyint", nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocuments", x => x.Id);
                    table.CheckConstraint("CK_StockDocuments_Posted", "([Status] = 3 AND [Number] IS NOT NULL AND [FiscalYear] IS NOT NULL AND [SequenceNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL AND [PostedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_StockDocuments_Reason", "([DocumentType] = 4 AND [AdjustmentReason] IS NOT NULL) OR ([DocumentType] <> 4 AND [AdjustmentReason] IS NULL)");
                    table.CheckConstraint("CK_StockDocuments_Reversal", "([DocumentType] = 5 AND [ReversalOfDocumentId] IS NOT NULL AND [RequiresApproval] = 1) OR ([DocumentType] <> 5 AND [ReversalOfDocumentId] IS NULL)");
                    table.CheckConstraint("CK_StockDocuments_Status", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_StockDocuments_Type", "[DocumentType] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_StockDocuments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocuments_StockDocuments_ReversalOfDocumentId",
                        column: x => x.ReversalOfDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocuments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "inventory",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Fingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecords_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OverReservationAlerts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandSellable = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverReservationAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverReservationAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OverReservationAlerts_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OverReservationAlerts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockDocumentEvents",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocumentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockDocumentEvents_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockDocumentLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ReversesLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocumentLines", x => x.Id);
                    table.CheckConstraint("CK_StockDocumentLines_Direction", "[Direction] IN (1, 2)");
                    table.CheckConstraint("CK_StockDocumentLines_Quantities", "[Quantity] > 0 AND [BaseQuantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_StockDocumentLines_UnitCost", "[UnitCost] IS NULL OR ([UnitCost] >= 0 AND [Direction] = 1)");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_StockDocumentLines_ReversesLineId",
                        column: x => x.ReversesLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostEntries",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValueDelta = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValueAfter = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AverageCostAfter = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostEntries", x => x.Id);
                    table.CheckConstraint("CK_InventoryCostEntries_After", "[QuantityAfter] >= 0 AND [ValueAfter] >= 0 AND [AverageCostAfter] >= 0");
                    table.CheckConstraint("CK_InventoryCostEntries_QuantityDelta", "[QuantityDelta] <> 0");
                    table.ForeignKey(
                        name: "FK_InventoryCostEntries_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostEntries_StockDocumentLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryCostEntries_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockAllocations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAllocations", x => x.Id);
                    table.CheckConstraint("CK_StockAllocations_From", "([FromWarehouseId] IS NULL AND [FromLocationId] IS NULL) OR ([FromWarehouseId] IS NOT NULL AND [FromLocationId] IS NOT NULL)");
                    table.CheckConstraint("CK_StockAllocations_OneSide", "[FromLocationId] IS NOT NULL OR [ToLocationId] IS NOT NULL");
                    table.CheckConstraint("CK_StockAllocations_Quantity", "[BaseQuantity] > 0");
                    table.CheckConstraint("CK_StockAllocations_To", "([ToWarehouseId] IS NULL AND [ToLocationId] IS NULL) OR ([ToWarehouseId] IS NOT NULL AND [ToLocationId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_StockAllocations_StockDocumentLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAllocations_WarehouseLocations_FromWarehouseId_FromLocationId",
                        columns: x => new { x.FromWarehouseId, x.FromLocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAllocations_WarehouseLocations_ToWarehouseId_ToLocationId",
                        columns: x => new { x.ToWarehouseId, x.ToLocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovements_Quantity", "[Quantity] <> 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "inventory",
                        principalTable: "StockAllocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_StockDocumentLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_WarehouseLocations_WarehouseId_LocationId",
                        columns: x => new { x.WarehouseId, x.LocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "inventory",
                table: "DocumentTypePolicies",
                columns: new[] { "DocumentType", "CreatedAtUtc", "CreatedByUserId", "RequiresApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { (byte)1, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null },
                    { (byte)2, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { (byte)3, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { (byte)4, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_BranchId",
                schema: "inventory",
                table: "DocumentNumberSequences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_DocumentId",
                schema: "inventory",
                table: "IdempotencyRecords",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostEntries_DocumentId",
                schema: "inventory",
                table: "InventoryCostEntries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostEntries_LineId",
                schema: "inventory",
                table: "InventoryCostEntries",
                column: "LineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostEntries_ProductId_Id",
                schema: "inventory",
                table: "InventoryCostEntries",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OverReservationAlerts_DocumentId",
                schema: "inventory",
                table: "OverReservationAlerts",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OverReservationAlerts_ProductId",
                schema: "inventory",
                table: "OverReservationAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OverReservationAlerts_WarehouseId_ProductId",
                schema: "inventory",
                table: "OverReservationAlerts",
                columns: new[] { "WarehouseId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseStock_WarehouseId",
                schema: "inventory",
                table: "ProductWarehouseStock",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_FromWarehouseId_FromLocationId",
                schema: "inventory",
                table: "StockAllocations",
                columns: new[] { "FromWarehouseId", "FromLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LineId",
                schema: "inventory",
                table: "StockAllocations",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_ToWarehouseId_ToLocationId",
                schema: "inventory",
                table: "StockAllocations",
                columns: new[] { "ToWarehouseId", "ToLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_WarehouseId_LocationId",
                schema: "inventory",
                table: "StockBalances",
                columns: new[] { "WarehouseId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockBalances_Key",
                schema: "inventory",
                table: "StockBalances",
                columns: new[] { "ProductId", "WarehouseId", "LocationId", "LotId", "StockStatus" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentEvents_DocumentId",
                schema: "inventory",
                table: "StockDocumentEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_DocumentId_LineNumber",
                schema: "inventory",
                table: "StockDocumentLines",
                columns: new[] { "DocumentId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_ProductId",
                schema: "inventory",
                table: "StockDocumentLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_ProductUnitId",
                schema: "inventory",
                table: "StockDocumentLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_ReversesLineId",
                schema: "inventory",
                table: "StockDocumentLines",
                column: "ReversesLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_UnitId",
                schema: "inventory",
                table: "StockDocumentLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_BranchId",
                schema: "inventory",
                table: "StockDocuments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_CreatedAtUtc",
                schema: "inventory",
                table: "StockDocuments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_DocumentType_BranchId_FiscalYear_SequenceNumber",
                schema: "inventory",
                table: "StockDocuments",
                columns: new[] { "DocumentType", "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_DraftNumber",
                schema: "inventory",
                table: "StockDocuments",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_Number",
                schema: "inventory",
                table: "StockDocuments",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_WarehouseId_Status",
                schema: "inventory",
                table: "StockDocuments",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_StockDocuments_OneReversal",
                schema: "inventory",
                table: "StockDocuments",
                column: "ReversalOfDocumentId",
                unique: true,
                filter: "[ReversalOfDocumentId] IS NOT NULL AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_AllocationId",
                schema: "inventory",
                table: "StockMovements",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_DocumentId",
                schema: "inventory",
                table: "StockMovements",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LineId",
                schema: "inventory",
                table: "StockMovements",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_WarehouseId_Id",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "ProductId", "WarehouseId", "Id" })
                .Annotation("SqlServer:Include", new[] { "LocationId", "Quantity" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId_LocationId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "WarehouseId", "LocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentNumberSequences",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "DocumentTypePolicies",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryCostEntries",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryCostStates",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "OverReservationAlerts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ProductWarehouseStock",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockBalances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockDocumentEvents",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockAllocations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockDocumentLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockDocuments",
                schema: "inventory");
        }
    }
}
