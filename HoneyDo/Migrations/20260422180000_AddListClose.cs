using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoneyDo.Migrations
{
    /// <inheritdoc />
    public partial class AddListClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "TodoLists",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "TodoLists");
        }
    }
}
