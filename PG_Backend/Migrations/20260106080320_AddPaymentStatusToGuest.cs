using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PG_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatusToGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaidDate",
                table: "Guests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Guests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPaidDate",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Guests");
        }
    }
}
