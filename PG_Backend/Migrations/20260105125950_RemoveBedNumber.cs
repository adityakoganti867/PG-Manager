using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PG_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBedNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BedNumber",
                table: "Guests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BedNumber",
                table: "Guests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
