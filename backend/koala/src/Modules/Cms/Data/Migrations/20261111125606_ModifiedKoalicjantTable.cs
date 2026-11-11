using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Cms.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedKoalicjantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "cms",
                table: "koalicjants");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "cms",
                table: "koalicjants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "cms",
                table: "koalicjants");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "cms",
                table: "koalicjants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
