using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration05_InventoryTransfersCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocumentLines_Direction",
                schema: "inventory",
                table: "StockDocumentLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentTypePolicies_Type",
                schema: "inventory",
                table: "DocumentTypePolicies");

            migrationBuilder.AddColumn<Guid>(
                name: "CountId",
                schema: "inventory",
                table: "StockDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                schema: "inventory",
                table: "StockDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                schema: "inventory",
                table: "IdempotencyRecords",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                schema: "inventory",
                table: "IdempotencyRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Every request recorded before this migration posted a stock document, which is its subject.
            migrationBuilder.Sql("UPDATE [inventory].[IdempotencyRecords] SET [SubjectId] = [DocumentId] WHERE [DocumentId] IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "StockCounts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsWholeWarehouse = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.CheckConstraint("CK_StockCounts_Status", "[Status] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_StockCounts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransitWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransitLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DispatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.CheckConstraint("CK_StockTransfers_Dispatched", "([Status] IN (1, 2, 8) AND [Number] IS NULL AND [TransitLocationId] IS NULL) OR ([Status] IN (3, 4, 5, 6, 7) AND [Number] IS NOT NULL AND [TransitLocationId] IS NOT NULL AND [DispatchedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_StockTransfers_Status", "[Status] BETWEEN 1 AND 8");
                    table.CheckConstraint("CK_StockTransfers_Warehouses", "[SourceWarehouseId] <> [DestinationWarehouseId] AND [TransitWarehouseId] <> [SourceWarehouseId] AND [TransitWarehouseId] <> [DestinationWarehouseId]");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_SourceBranchId",
                        column: x => x.SourceBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_WarehouseLocations_TransitWarehouseId_TransitLocationId",
                        columns: x => new { x.TransitWarehouseId, x.TransitLocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_TransitWarehouseId",
                        column: x => x.TransitWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountEvents",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountEvents_StockCounts_CountId",
                        column: x => x.CountId,
                        principalSchema: "inventory",
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.CheckConstraint("CK_StockCountLines_Quantities", "[ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0) AND ([UnitCost] IS NULL OR [UnitCost] >= 0)");
                    table.ForeignKey(
                        name: "FK_StockCountLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCounts_CountId",
                        column: x => x.CountId,
                        principalSchema: "inventory",
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockCountLines_WarehouseLocations_WarehouseId_LocationId",
                        columns: x => new { x.WarehouseId, x.LocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountScopes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountScopes_StockCounts_CountId",
                        column: x => x.CountId,
                        principalSchema: "inventory",
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockCountScopes_WarehouseLocations_WarehouseId_LocationId",
                        columns: x => new { x.WarehouseId, x.LocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountScopes_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferEvents",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferEvents_StockTransfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "inventory",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LostQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLines", x => x.Id);
                    table.CheckConstraint("CK_StockTransferLines_Quantities", "[Quantity] > 0 AND [BaseQuantity] > 0 AND [ConversionFactor] > 0 AND [ReceivedQuantity] >= 0 AND [LostQuantity] >= 0 AND [ReceivedQuantity] + [LostQuantity] <= [BaseQuantity]");
                    table.ForeignKey(
                        name: "FK_StockTransferLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_StockTransfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "inventory",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_WarehouseLocations_SourceWarehouseId_SourceLocationId",
                        columns: x => new { x.SourceWarehouseId, x.SourceLocationId },
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "inventory",
                table: "DocumentTypePolicies",
                columns: new[] { "DocumentType", "CreatedAtUtc", "CreatedByUserId", "RequiresApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { (byte)6, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_CountId",
                schema: "inventory",
                table: "StockDocuments",
                column: "CountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_TransferId",
                schema: "inventory",
                table: "StockDocuments",
                column: "TransferId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 8");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocumentLines_Direction",
                schema: "inventory",
                table: "StockDocumentLines",
                sql: "[Direction] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "inventory",
                table: "IdempotencyRecords",
                column: "SubjectId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentTypePolicies_Type",
                schema: "inventory",
                table: "DocumentTypePolicies",
                sql: "[DocumentType] IN (1, 2, 3, 4, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountEvents_CountId",
                schema: "inventory",
                table: "StockCountEvents",
                column: "CountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_CountId_ProductId_LocationId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "CountId", "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ProductId",
                schema: "inventory",
                table: "StockCountLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_WarehouseId_LocationId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "WarehouseId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_BranchId",
                schema: "inventory",
                table: "StockCounts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CreatedAtUtc",
                schema: "inventory",
                table: "StockCounts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_DraftNumber",
                schema: "inventory",
                table: "StockCounts",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_WarehouseId_Status",
                schema: "inventory",
                table: "StockCounts",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountScopes_CountId",
                schema: "inventory",
                table: "StockCountScopes",
                column: "CountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountScopes_WarehouseId_IsActive",
                schema: "inventory",
                table: "StockCountScopes",
                columns: new[] { "WarehouseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountScopes_WarehouseId_LocationId",
                schema: "inventory",
                table: "StockCountScopes",
                columns: new[] { "WarehouseId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockCountScopes_ActiveLocation",
                schema: "inventory",
                table: "StockCountScopes",
                column: "LocationId",
                unique: true,
                filter: "[IsActive] = 1 AND [LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_StockCountScopes_ActiveWarehouse",
                schema: "inventory",
                table: "StockCountScopes",
                column: "WarehouseId",
                unique: true,
                filter: "[IsActive] = 1 AND [LocationId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferEvents_TransferId",
                schema: "inventory",
                table: "StockTransferEvents",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_ProductId",
                schema: "inventory",
                table: "StockTransferLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_ProductUnitId",
                schema: "inventory",
                table: "StockTransferLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_SourceWarehouseId_SourceLocationId",
                schema: "inventory",
                table: "StockTransferLines",
                columns: new[] { "SourceWarehouseId", "SourceLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_TransferId_LineNumber",
                schema: "inventory",
                table: "StockTransferLines",
                columns: new[] { "TransferId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_UnitId",
                schema: "inventory",
                table: "StockTransferLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CreatedAtUtc",
                schema: "inventory",
                table: "StockTransfers",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationBranchId",
                schema: "inventory",
                table: "StockTransfers",
                column: "DestinationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationWarehouseId_Status",
                schema: "inventory",
                table: "StockTransfers",
                columns: new[] { "DestinationWarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DraftNumber",
                schema: "inventory",
                table: "StockTransfers",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Number",
                schema: "inventory",
                table: "StockTransfers",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceBranchId",
                schema: "inventory",
                table: "StockTransfers",
                column: "SourceBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceWarehouseId_Status",
                schema: "inventory",
                table: "StockTransfers",
                columns: new[] { "SourceWarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransitWarehouseId_TransitLocationId",
                schema: "inventory",
                table: "StockTransfers",
                columns: new[] { "TransitWarehouseId", "TransitLocationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockDocuments_StockCounts_CountId",
                schema: "inventory",
                table: "StockDocuments",
                column: "CountId",
                principalSchema: "inventory",
                principalTable: "StockCounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDocuments_StockTransfers_TransferId",
                schema: "inventory",
                table: "StockDocuments",
                column: "TransferId",
                principalSchema: "inventory",
                principalTable: "StockTransfers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocuments_StockCounts_CountId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocuments_StockTransfers_TransferId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropTable(
                name: "StockCountEvents",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCountLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCountScopes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockTransferEvents",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockTransferLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCounts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_StockDocuments_CountId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StockDocuments_TransferId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocumentLines_Direction",
                schema: "inventory",
                table: "StockDocumentLines");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "inventory",
                table: "IdempotencyRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentTypePolicies_Type",
                schema: "inventory",
                table: "DocumentTypePolicies");

            migrationBuilder.DeleteData(
                schema: "inventory",
                table: "DocumentTypePolicies",
                keyColumn: "DocumentType",
                keyValue: (byte)6);

            migrationBuilder.DropColumn(
                name: "CountId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropColumn(
                name: "TransferId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                schema: "inventory",
                table: "IdempotencyRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                schema: "inventory",
                table: "IdempotencyRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocumentLines_Direction",
                schema: "inventory",
                table: "StockDocumentLines",
                sql: "[Direction] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentTypePolicies_Type",
                schema: "inventory",
                table: "DocumentTypePolicies",
                sql: "[DocumentType] BETWEEN 1 AND 4");
        }
    }
}
