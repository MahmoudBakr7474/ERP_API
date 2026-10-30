using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration17_CompanyContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                schema: "organization",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                schema: "organization",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistrationNumber",
                schema: "organization",
                table: "Companies",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistry",
                schema: "organization",
                table: "Companies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "organization",
                table: "Companies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "organization",
                table: "Companies",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CityId",
                schema: "organization",
                table: "Companies",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Cities_CityId",
                schema: "organization",
                table: "Companies",
                column: "CityId",
                principalSchema: "organization",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Cities_CityId",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CityId",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CityId",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommercialRegistrationNumber",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommercialRegistry",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "organization",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "organization",
                table: "Companies");
        }
    }
}
