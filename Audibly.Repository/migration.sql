CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Audiobooks" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Audiobooks" PRIMARY KEY,
    "Author" TEXT NOT NULL,
    "Composer" TEXT NOT NULL,
    "CurrentSourceFileIndex" INTEGER NOT NULL,
    "DateLastPlayed" TEXT NULL,
    "Description" TEXT NOT NULL,
    "CoverImagePath" TEXT NOT NULL,
    "ThumbnailPath" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "IsNowPlaying" INTEGER NOT NULL,
    "PlaybackSpeed" REAL NOT NULL,
    "Progress" REAL NOT NULL,
    "ReleaseDate" TEXT NULL,
    "Title" TEXT NOT NULL,
    "Volume" REAL NOT NULL
);

CREATE TABLE "SourceFile" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SourceFile" PRIMARY KEY,
    "Index" INTEGER NOT NULL,
    "FilePath" TEXT NOT NULL,
    "CurrentTimeMs" INTEGER NOT NULL,
    "Duration" INTEGER NOT NULL,
    "CurrentChapterIndex" INTEGER NULL,
    "AudiobookId" TEXT NULL,
    CONSTRAINT "FK_SourceFile_Audiobooks_AudiobookId" FOREIGN KEY ("AudiobookId") REFERENCES "Audiobooks" ("Id")
);

CREATE TABLE "Chapters" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Chapters" PRIMARY KEY,
    "Index" INTEGER NOT NULL,
    "StartTime" INTEGER NOT NULL,
    "EndTime" INTEGER NOT NULL,
    "StartOffset" INTEGER NOT NULL,
    "EndOffset" INTEGER NOT NULL,
    "UseOffset" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "UniqueID" TEXT NOT NULL,
    "Subtitle" TEXT NOT NULL,
    "SourceFileId" TEXT NULL,
    CONSTRAINT "FK_Chapters_SourceFile_SourceFileId" FOREIGN KEY ("SourceFileId") REFERENCES "SourceFile" ("Id")
);

CREATE UNIQUE INDEX "IX_Audiobooks_Author_Title" ON "Audiobooks" ("Author", "Title");

CREATE INDEX "IX_Chapters_SourceFileId" ON "Chapters" ("SourceFileId");

CREATE INDEX "IX_SourceFile_AudiobookId" ON "SourceFile" ("AudiobookId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241003053944_AddSourceFilesTable', '8.0.3');

COMMIT;

