using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PG_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateComplaintColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedResolutionDate",
                table: "Complaints");

            migrationBuilder.RenameColumn(
                name: "AdminComment",
                table: "Complaints",
                newName: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Complaints",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedResolutionDays",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedResolutionDays",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Complaints");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Complaints",
                newName: "AdminComment");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(600)",
                oldMaxLength: 600);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedResolutionDate",
                table: "Complaints",
                type: "datetime2",
                nullable: true);
        }
    }
}
