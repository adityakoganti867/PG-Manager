using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PG_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndPropertyForLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Properties",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnerName",
                value: "Aditya Koganti");

            migrationBuilder.UpdateData(
                table: "PropertySettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "Value",
                value: "Ramesh Kumar");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Username",
                value: "admin");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 100,
                column: "Username",
                value: "adityarajat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "Properties");

            migrationBuilder.UpdateData(
                table: "PropertySettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "Value",
                value: "Aditya Koganti");
        }
    }
}
