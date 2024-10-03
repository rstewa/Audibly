using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audibly.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Audiobooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Composer = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentSourceFileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    DateLastPlayed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    CoverImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    IsNowPlaying = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlaybackSpeed = table.Column<double>(type: "REAL", nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Volume = table.Column<double>(type: "REAL", nullable: false),
                    CurrentChapterIndex = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audiobooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrentChapterIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    AudiobookId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceFile_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<uint>(type: "INTEGER", nullable: false),
                    EndTime = table.Column<uint>(type: "INTEGER", nullable: false),
                    StartOffset = table.Column<uint>(type: "INTEGER", nullable: false),
                    EndOffset = table.Column<uint>(type: "INTEGER", nullable: false),
                    UseOffset = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    UniqueID = table.Column<string>(type: "TEXT", nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: false),
                    AudiobookId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceFileId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Chapters_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "SourceFile",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audiobooks_Author_Title",
                table: "Audiobooks",
                columns: new[] { "Author", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_AudiobookId",
                table: "Chapters",
                column: "AudiobookId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SourceFileId",
                table: "Chapters",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceFile_AudiobookId",
                table: "SourceFile",
                column: "AudiobookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "SourceFile");

            migrationBuilder.DropTable(
                name: "Audiobooks");
        }
    }
}
