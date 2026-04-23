using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoneyDo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProfileDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Profiles",
                type: "TEXT",
                nullable: true);
        }
    }
}
