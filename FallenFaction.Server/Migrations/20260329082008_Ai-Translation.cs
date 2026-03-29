using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AiTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterCount",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAILocked",
                table: "Chapters",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateTable(
                name: "AIChapterUnlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    UnlockedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TicketCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TicketTypeUsed = table.Column<int>(type: "int", nullable: false),
                    CharacterCount = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChapterUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChapterUnlocks_AspNetUsers_UnlockedByUserId",
                        column: x => x.UnlockedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIChapterUnlocks_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIChapterUnlocks_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TicketType = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedTitleId = table.Column<int>(type: "int", nullable: true),
                    RelatedChapterId = table.Column<int>(type: "int", nullable: true),
                    RelatedRequestId = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatreonTierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTransactions_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranslationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProposedTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalLanguageTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Genres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedChapterCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReleasedTitleId = table.Column<int>(type: "int", nullable: true),
                    ReleaseTicketCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranslationRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TranslationRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TranslationRequests_Titles_ReleasedTitleId",
                        column: x => x.ReleasedTitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GoldBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SilverBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIChapterUnlocks_ChapterId",
                table: "AIChapterUnlocks",
                column: "ChapterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIChapterUnlocks_TitleId",
                table: "AIChapterUnlocks",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChapterUnlocks_UnlockedByUserId",
                table: "AIChapterUnlocks",
                column: "UnlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_CreatedAt",
                table: "TicketTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_ExpiresAt",
                table: "TicketTransactions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_PerformedByUserId",
                table: "TicketTransactions",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_UserId",
                table: "TicketTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_UserId_Type",
                table: "TicketTransactions",
                columns: new[] { "UserId", "TicketType" });

            migrationBuilder.CreateIndex(
                name: "IX_TranslationRequests_CreatedAt",
                table: "TranslationRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationRequests_ReleasedTitleId",
                table: "TranslationRequests",
                column: "ReleasedTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationRequests_RequestedBy",
                table: "TranslationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationRequests_ReviewedByUserId",
                table: "TranslationRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationRequests_Status",
                table: "TranslationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserTickets_UserId",
                table: "UserTickets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIChapterUnlocks");

            migrationBuilder.DropTable(
                name: "TicketTransactions");

            migrationBuilder.DropTable(
                name: "TranslationRequests");

            migrationBuilder.DropTable(
                name: "UserTickets");

            migrationBuilder.DropColumn(
                name: "CharacterCount",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "IsAILocked",
                table: "Chapters");

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
    }
}
