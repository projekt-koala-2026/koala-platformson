using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Cms.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToPublicFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "cms",
                table: "public_files",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                schema: "cms",
                table: "public_files");
        }
    }
}
