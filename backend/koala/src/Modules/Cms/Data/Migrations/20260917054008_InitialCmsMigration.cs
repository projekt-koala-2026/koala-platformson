using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace koala.src.Modules.Cms.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCmsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.CreateTable(
                name: "Koalicjants",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_first = table.Column<string>(type: "text", nullable: false),
                    name_last = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_visiable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Koalicjants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_visable = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "public_files",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_visiable = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "static_pages",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_static_pages", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "cms",
                table: "static_pages",
                columns: new[] { "id", "content_json", "name", "updated_at", "version" },
                values: new object[,]
                {
                    { new Guid("01a065c3-3bec-7224-b7b2-abc51fe1d87f"), "{}", "HOME_PAGE", new DateTime(2026, 7, 6, 7, 6, 7, 0, DateTimeKind.Utc), 0 },
                    { new Guid("01a065c3-3bec-7224-b7b2-afa2801aa4a4"), "{}", "TASKS_PAGE", new DateTime(2026, 7, 6, 7, 6, 7, 0, DateTimeKind.Utc), 0 },
                    { new Guid("01a065c3-3bec-7224-b7b2-b241dc642361"), "{}", "HISTORY_PAGE", new DateTime(2026, 7, 6, 7, 6, 7, 0, DateTimeKind.Utc), 0 },
                    { new Guid("01a065c3-3bec-7224-b7b2-b426015c004a"), "{}", "RULES_PAGE", new DateTime(2026, 7, 6, 7, 6, 7, 0, DateTimeKind.Utc), 0 },
                    { new Guid("01a065c3-3bec-7224-b7b2-b86c61973289"), "{}", "KOALICJANTS_PAGE", new DateTime(2026, 7, 6, 7, 6, 7, 0, DateTimeKind.Utc), 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Koalicjants",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "Posts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "public_files",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "Sponsors",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "static_pages",
                schema: "cms");
        }
    }
}
