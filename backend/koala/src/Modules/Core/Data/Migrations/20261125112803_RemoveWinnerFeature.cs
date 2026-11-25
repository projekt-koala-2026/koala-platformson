using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWinnerFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_editions_winners_winner_tier1_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropForeignKey(
                name: "FK_editions_winners_winner_tier2_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropForeignKey(
                name: "FK_Etap1Series_editions_edition_id",
                schema: "core",
                table: "Etap1Series");

            migrationBuilder.DropForeignKey(
                name: "FK_rankings_Etap1Series_Etap1SeriesId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_Etap1Series_Etap1Series_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "winners",
                schema: "core");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Etap1Series",
                schema: "core",
                table: "Etap1Series");

            migrationBuilder.DropIndex(
                name: "IX_editions_winner_tier1_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropIndex(
                name: "IX_editions_winner_tier2_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "are_winners_public",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "winner_tier1_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "winner_tier2_id",
                schema: "core",
                table: "editions");

            migrationBuilder.RenameTable(
                name: "Etap1Series",
                schema: "core",
                newName: "etap1series",
                newSchema: "core");

            migrationBuilder.RenameIndex(
                name: "IX_Etap1Series_edition_id",
                schema: "core",
                table: "etap1series",
                newName: "IX_etap1series_edition_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_etap1series",
                schema: "core",
                table: "etap1series",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_etap1series_editions_edition_id",
                schema: "core",
                table: "etap1series",
                column: "edition_id",
                principalSchema: "core",
                principalTable: "editions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rankings_etap1series_Etap1SeriesId",
                schema: "core",
                table: "rankings",
                column: "Etap1SeriesId",
                principalSchema: "core",
                principalTable: "etap1series",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_etap1series_Etap1Series_id",
                schema: "core",
                table: "tasks",
                column: "Etap1Series_id",
                principalSchema: "core",
                principalTable: "etap1series",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_etap1series_editions_edition_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropForeignKey(
                name: "FK_rankings_etap1series_Etap1SeriesId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_etap1series_Etap1Series_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_etap1series",
                schema: "core",
                table: "etap1series");

            migrationBuilder.RenameTable(
                name: "etap1series",
                schema: "core",
                newName: "Etap1Series",
                newSchema: "core");

            migrationBuilder.RenameIndex(
                name: "IX_etap1series_edition_id",
                schema: "core",
                table: "Etap1Series",
                newName: "IX_Etap1Series_edition_id");

            migrationBuilder.AddColumn<bool>(
                name: "are_winners_public",
                schema: "core",
                table: "editions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "winner_tier1_id",
                schema: "core",
                table: "editions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "winner_tier2_id",
                schema: "core",
                table: "editions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Etap1Series",
                schema: "core",
                table: "Etap1Series",
                column: "id");

            migrationBuilder.CreateTable(
                name: "winners",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    EditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    team_name = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_winners", x => x.id);
                    table.ForeignKey(
                        name: "FK_winners_editions_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "core",
                        principalTable: "editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_editions_winner_tier1_id",
                schema: "core",
                table: "editions",
                column: "winner_tier1_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_editions_winner_tier2_id",
                schema: "core",
                table: "editions",
                column: "winner_tier2_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_winners_EditionId",
                schema: "core",
                table: "winners",
                column: "EditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_editions_winners_winner_tier1_id",
                schema: "core",
                table: "editions",
                column: "winner_tier1_id",
                principalSchema: "core",
                principalTable: "winners",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_editions_winners_winner_tier2_id",
                schema: "core",
                table: "editions",
                column: "winner_tier2_id",
                principalSchema: "core",
                principalTable: "winners",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Etap1Series_editions_edition_id",
                schema: "core",
                table: "Etap1Series",
                column: "edition_id",
                principalSchema: "core",
                principalTable: "editions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rankings_Etap1Series_Etap1SeriesId",
                schema: "core",
                table: "rankings",
                column: "Etap1SeriesId",
                principalSchema: "core",
                principalTable: "Etap1Series",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_Etap1Series_Etap1Series_id",
                schema: "core",
                table: "tasks",
                column: "Etap1Series_id",
                principalSchema: "core",
                principalTable: "Etap1Series",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
