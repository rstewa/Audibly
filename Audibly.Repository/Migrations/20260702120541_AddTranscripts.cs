using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audibly.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscripts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranscriptChapterStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AudiobookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceFileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptChapterStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptChapterStatuses_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AudiobookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceFileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartMs = table.Column<int>(type: "INTEGER", nullable: false),
                    EndMs = table.Column<int>(type: "INTEGER", nullable: false),
                    IsParagraphStart = table.Column<bool>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    WordTimings = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSegments_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptChapterStatuses_AudiobookId_ChapterIndex",
                table: "TranscriptChapterStatuses",
                columns: new[] { "AudiobookId", "ChapterIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_AudiobookId_ChapterIndex_StartMs",
                table: "TranscriptSegments",
                columns: new[] { "AudiobookId", "ChapterIndex", "StartMs" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptChapterStatuses");

            migrationBuilder.DropTable(
                name: "TranscriptSegments");
        }
    }
}
