using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audibly.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Audiobooks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audiobooks_FolderId",
                table: "Audiobooks",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Name_ParentFolderId",
                table: "Folders",
                columns: new[] { "Name", "ParentFolderId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Audiobooks_Folders_FolderId",
                table: "Audiobooks",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audiobooks_Folders_FolderId",
                table: "Audiobooks");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Audiobooks_FolderId",
                table: "Audiobooks");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Audiobooks");
        }
    }
}
