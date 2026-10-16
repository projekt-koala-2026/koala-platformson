using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Cms.Data.Migrations
{
    /// <inheritdoc />
    public partial class SomePendingChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Sponsors",
                schema: "cms",
                table: "Sponsors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                schema: "cms",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Koalicjants",
                schema: "cms",
                table: "Koalicjants");

            migrationBuilder.RenameTable(
                name: "Sponsors",
                schema: "cms",
                newName: "sponsors",
                newSchema: "cms");

            migrationBuilder.RenameTable(
                name: "Posts",
                schema: "cms",
                newName: "posts",
                newSchema: "cms");

            migrationBuilder.RenameTable(
                name: "Koalicjants",
                schema: "cms",
                newName: "koalicjants",
                newSchema: "cms");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sponsors",
                schema: "cms",
                table: "sponsors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_posts",
                schema: "cms",
                table: "posts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_koalicjants",
                schema: "cms",
                table: "koalicjants",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_sponsors",
                schema: "cms",
                table: "sponsors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_posts",
                schema: "cms",
                table: "posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_koalicjants",
                schema: "cms",
                table: "koalicjants");

            migrationBuilder.RenameTable(
                name: "sponsors",
                schema: "cms",
                newName: "Sponsors",
                newSchema: "cms");

            migrationBuilder.RenameTable(
                name: "posts",
                schema: "cms",
                newName: "Posts",
                newSchema: "cms");

            migrationBuilder.RenameTable(
                name: "koalicjants",
                schema: "cms",
                newName: "Koalicjants",
                newSchema: "cms");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sponsors",
                schema: "cms",
                table: "Sponsors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                schema: "cms",
                table: "Posts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Koalicjants",
                schema: "cms",
                table: "Koalicjants",
                column: "id");
        }
    }
}
