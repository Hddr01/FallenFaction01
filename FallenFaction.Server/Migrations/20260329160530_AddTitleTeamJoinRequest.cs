using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleTeamJoinRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TitleTeamJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    RequestingTeamId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AutoRejectedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleTeamJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TitleTeamJoinRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TitleTeamJoinRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TitleTeamJoinRequests_Teams_RequestingTeamId",
                        column: x => x.RequestingTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TitleTeamJoinRequests_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TitleTeamJoinRequests_RequestedByUserId",
                table: "TitleTeamJoinRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTeamJoinRequests_RequestingTeamId",
                table: "TitleTeamJoinRequests",
                column: "RequestingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTeamJoinRequests_ReviewedByUserId",
                table: "TitleTeamJoinRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTeamJoinRequests_TitleId_TeamId_Status",
                table: "TitleTeamJoinRequests",
                columns: new[] { "TitleId", "RequestingTeamId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TitleTeamJoinRequests");
        }
    }
}
