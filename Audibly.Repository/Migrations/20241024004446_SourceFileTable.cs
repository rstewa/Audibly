using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audibly.Repository.Migrations
{
    public partial class SourceFileTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // this is disgusting and I fucked up not using a migration when I initially created the database
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS Audiobooks (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Author TEXT NOT NULL,
                    Composer TEXT NOT NULL,
                    DateLastPlayed TEXT,
                    Description TEXT NOT NULL,
                    Duration INTEGER NOT NULL,
                    CurrentTimeMs INTEGER NOT NULL,
                    CoverImagePath TEXT NOT NULL,
                    ThumbnailPath TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    IsNowPlaying INTEGER NOT NULL,
                    PlaybackSpeed REAL NOT NULL,
                    Progress REAL NOT NULL,
                    ReleaseDate TEXT,
                    Title TEXT NOT NULL,
                    Volume REAL NOT NULL,
                    CurrentChapterIndex INTEGER
                );

                CREATE TABLE IF NOT EXISTS Chapters (
                    Id TEXT NOT NULL PRIMARY KEY,
                    ""Index"" INTEGER NOT NULL,
                    StartTime INTEGER NOT NULL,
                    EndTime INTEGER NOT NULL,
                    StartOffset INTEGER NOT NULL,
                    EndOffset INTEGER NOT NULL,
                    UseOffset INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    UniqueID TEXT NOT NULL,
                    Subtitle TEXT NOT NULL,
                    AudiobookId TEXT,
                    FOREIGN KEY (AudiobookId) REFERENCES Audiobooks (Id)
                );
                
                CREATE INDEX IF NOT EXISTS IX_Audiobooks_Author_Title ON Audiobooks (Author, Title);
                CREATE INDEX IF NOT EXISTS IX_Chapters_AudiobookId ON Chapters (AudiobookId);
            ");
            
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Audiobooks_AudiobookId",
                table: "Chapters");
            
            migrationBuilder.AlterColumn<Guid>(
                name: "AudiobookId",
                table: "Chapters",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentSourceFileIndex",
                table: "Chapters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);            
            
            migrationBuilder.Sql(@"
                CREATE TABLE SourceFiles (
                    Id TEXT PRIMARY KEY NOT NULL,
                    ""Index"" INTEGER NOT NULL,
                    FilePath TEXT NOT NULL,
                    CurrentTimeMs INTEGER NOT NULL,
                    Duration INTEGER NOT NULL,
                    AudiobookId TEXT NOT NULL,
                    FOREIGN KEY (AudiobookId) REFERENCES Audiobooks(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_SourceFiles_AudiobookId ON SourceFiles (AudiobookId);
                
                -- create needed source file records
                INSERT INTO SourceFiles (Id, ""Index"", FilePath, CurrentTimeMs, Duration, AudiobookId)
                SELECT hex(randomblob(16)), 0, FilePath, CurrentTimeMs, Duration, Id FROM Audiobooks;
                
                -- changes to audiobooks table
                ALTER TABLE Audiobooks DROP COLUMN FilePath;
                ALTER TABLE Audiobooks DROP COLUMN CurrentTimeMs;
                ALTER TABLE Audiobooks ADD COLUMN CurrentSourceFileIndex INTEGER NOT NULL DEFAULT 0;"
            );
            
            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Audiobooks_AudiobookId",
                table: "Chapters",
                column: "AudiobookId",
                principalTable: "Audiobooks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceFiles");

            migrationBuilder.DropColumn(
                name: "ParentSourceFileIndex",
                table: "Chapters");

            migrationBuilder.RenameColumn(
                name: "CurrentSourceFileIndex",
                table: "Audiobooks",
                newName: "CurrentTimeMs");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Audiobooks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}