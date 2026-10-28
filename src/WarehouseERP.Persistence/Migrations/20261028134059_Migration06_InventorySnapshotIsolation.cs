using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <summary>
    /// Lets the Inventory reconciliation report read every table in one SNAPSHOT transaction, so postings committing
    /// meanwhile can't show as false differences; everything else keeps READ COMMITTED. ALTER DATABASE can't run inside
    /// a migration's transaction, so it is alone here (no partly applied migration if it fails), and it waits for open
    /// transactions to finish. On a shared server a DBA must allow it.
    /// </summary>
    public partial class Migration06_InventorySnapshotIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION OFF;", suppressTransaction: true);
        }
    }
}
