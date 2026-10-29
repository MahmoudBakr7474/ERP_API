using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration11_InventoryPos : Migration
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments",
                sql: "([DocumentType] IN (9, 10, 11, 12, 13, 14) AND [SourceDocumentId] IS NOT NULL) OR ([DocumentType] NOT IN (9, 10, 11, 12, 13, 14) AND [SourceDocumentId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 14");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                sql: "([DocumentType] IN (9, 10, 11, 12) AND [SourceDocumentId] IS NOT NULL) OR ([DocumentType] NOT IN (9, 10, 11, 12) AND [SourceDocumentId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 12");
        }
    }
}
