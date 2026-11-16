using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTestDescriptionProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TestDescription",
                table: "Test1",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestDescription",
                table: "Test1");
        }
    }
}
