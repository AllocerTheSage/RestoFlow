using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTableCategoriesAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tables");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Tables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TableCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tables_CategoryId",
                table: "Tables",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_TableCategories_CategoryId",
                table: "Tables",
                column: "CategoryId",
                principalTable: "TableCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_TableCategories_CategoryId",
                table: "Tables");

            migrationBuilder.DropTable(
                name: "TableCategories");

            migrationBuilder.DropIndex(
                name: "IX_Tables_CategoryId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tables");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tables",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
