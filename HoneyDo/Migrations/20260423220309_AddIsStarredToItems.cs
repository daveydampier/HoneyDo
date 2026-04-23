using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoneyDo.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStarredToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "TodoItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "TodoItems");
        }
    }
}
