using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranslationGlossaryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    SourceTerm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Translation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    FirstSeenChapter = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationGlossaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranslationGlossaryEntries_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranslationTitleProfiles",
                columns: table => new
                {
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    SourceLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StyleGuide = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Synopsis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequireReview = table.Column<bool>(type: "bit", nullable: false),
                    MinQaScore = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationTitleProfiles", x => x.TitleId);
                    table.ForeignKey(
                        name: "FK_TranslationTitleProfiles_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranslationWorkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationWorkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranslationJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    VolumeNumber = table.Column<int>(type: "int", nullable: false),
                    ChapterNumber = table.Column<int>(type: "int", nullable: false),
                    SourceChapterTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimedByWorkerId = table.Column<int>(type: "int", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TranslatedTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TranslatedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    QaReport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QaScore = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    ModelInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResultChapterId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranslationJobs_Chapters_ResultChapterId",
                        column: x => x.ResultChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TranslationJobs_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranslationJobs_TranslationWorkers_ClaimedByWorkerId",
                        column: x => x.ClaimedByWorkerId,
                        principalTable: "TranslationWorkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranslationGlossary_Title_Term",
                table: "TranslationGlossaryEntries",
                columns: new[] { "TitleId", "SourceTerm" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranslationJobs_ClaimedByWorkerId",
                table: "TranslationJobs",
                column: "ClaimedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationJobs_ResultChapterId",
                table: "TranslationJobs",
                column: "ResultChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationJobs_Status_Lease",
                table: "TranslationJobs",
                columns: new[] { "Status", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TranslationJobs_Title_Vol_Ch",
                table: "TranslationJobs",
                columns: new[] { "TitleId", "VolumeNumber", "ChapterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranslationWorkers_KeyHash",
                table: "TranslationWorkers",
                column: "KeyHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranslationGlossaryEntries");

            migrationBuilder.DropTable(
                name: "TranslationJobs");

            migrationBuilder.DropTable(
                name: "TranslationTitleProfiles");

            migrationBuilder.DropTable(
                name: "TranslationWorkers");
        }
    }
}
