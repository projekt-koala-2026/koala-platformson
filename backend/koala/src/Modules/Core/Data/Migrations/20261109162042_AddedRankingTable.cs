using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedRankingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rankings",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Etap1Series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "text", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    points_max = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rankings", x => x.id);
                    table.ForeignKey(
                        name: "FK_rankings_Etap1Series_Etap1Series_id",
                        column: x => x.Etap1Series_id,
                        principalSchema: "core",
                        principalTable: "Etap1Series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rankings_Etap1Series_id",
                schema: "core",
                table: "rankings",
                column: "Etap1Series_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rankings",
                schema: "core");
        }
    }
}
