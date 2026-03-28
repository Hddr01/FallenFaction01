

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class Updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceTitleName",
                table: "Titles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTitleName",
                table: "PendingTitles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceTitleName",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "SourceTitleName",
                table: "PendingTitles");
        }
    }
}
