using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PG_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRentAndJoinDateToGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoiningDate",
                table: "Guests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "RentAmount",
                table: "Guests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoiningDate",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "RentAmount",
                table: "Guests");
        }
    }
}
