using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemovePatreonOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatreonAccessToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatreonLinkedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatreonMonthlyAmount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatreonRefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatreonTierName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatreonUserId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatreonAccessToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PatreonLinkedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PatreonMonthlyAmount",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PatreonRefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatreonTierName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatreonUserId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
