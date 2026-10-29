using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration09_InventoryReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.CreateTable(
                name: "StockReservations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservations", x => x.Id);
                    table.CheckConstraint("CK_StockReservations_Quantities", "[ReservedQuantity] > 0 AND [ConsumedQuantity] >= 0 AND [ReleasedQuantity] >= 0 AND [ConsumedQuantity] + [ReleasedQuantity] <= [ReservedQuantity]");
                    table.ForeignKey(
                        name: "FK_StockReservations_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReservationEntries",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservationEntries", x => x.Id);
                    table.CheckConstraint("CK_StockReservationEntries_Document", "([Kind] = 2 AND [StockDocumentId] IS NOT NULL) OR ([Kind] <> 2 AND [StockDocumentId] IS NULL)");
                    table.CheckConstraint("CK_StockReservationEntries_Kind", "[Kind] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_StockReservationEntries_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_StockReservationEntries_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockReservationEntries_StockReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "inventory",
                        principalTable: "StockReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments",
                sql: "([DocumentType] IN (9, 10, 11, 12) AND [SourceDocumentId] IS NOT NULL) OR ([DocumentType] NOT IN (9, 10, 11, 12) AND [SourceDocumentId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 12");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservationEntries_ReservationId",
                schema: "inventory",
                table: "StockReservationEntries",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservationEntries_StockDocumentId",
                schema: "inventory",
                table: "StockReservationEntries",
                column: "StockDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ProductId",
                schema: "inventory",
                table: "StockReservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_SourceDocumentId",
                schema: "inventory",
                table: "StockReservations",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_SourceLineId",
                schema: "inventory",
                table: "StockReservations",
                column: "SourceLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_WarehouseId_ProductId",
                schema: "inventory",
                table: "StockReservations",
                columns: new[] { "WarehouseId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockReservationEntries",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockReservations",
                schema: "inventory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments",
                sql: "([DocumentType] IN (9, 10) AND [SourceDocumentId] IS NOT NULL) OR ([DocumentType] NOT IN (9, 10) AND [SourceDocumentId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 10");
        }
    }
}
