using System;
using Audibly.Repository.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audibly.Repository.Migrations
{
    [DbContext(typeof(AudiblyContext))]
    [Migration("20261016125635_AddPlaybackBookmarks")]
    public partial class AddPlaybackBookmarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaybackBookmark",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AudiobookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceFileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ChapterTitle = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackBookmark", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaybackBookmark_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackBookmark_AudiobookId",
                table: "PlaybackBookmark",
                column: "AudiobookId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlaybackBookmark");
        }
    }
}
