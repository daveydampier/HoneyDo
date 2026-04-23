using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoneyDo.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "ActivityLogs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Detail",
                table: "ActivityLogs");
        }
    }
}
