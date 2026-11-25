using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyRankingsSoTheyOnlySupportEtap1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rankings_editions_edition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropForeignKey(
                name: "FK_rankings_etap1series_Etap1SeriesId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropIndex(
                name: "IX_rankings_edition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropIndex(
                name: "IX_rankings_Etap1SeriesId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "Etap1SeriesId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "edition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "is_public",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "tier",
                schema: "core",
                table: "rankings");

            migrationBuilder.RenameColumn(
                name: "ranking_published",
                schema: "core",
                table: "etap1series",
                newName: "are_rankings_public");

            migrationBuilder.AddColumn<bool>(
                name: "has_qualified_to_next_etap",
                schema: "core",
                table: "ranking_datas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "tier1_ranking_id",
                schema: "core",
                table: "etap1series",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tier2_ranking_id",
                schema: "core",
                table: "etap1series",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "are_rankings_public",
                schema: "core",
                table: "editions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "tier1_ranking_total_id",
                schema: "core",
                table: "editions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tier2_ranking_total_id",
                schema: "core",
                table: "editions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_etap1series_tier1_ranking_id",
                schema: "core",
                table: "etap1series",
                column: "tier1_ranking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etap1series_tier2_ranking_id",
                schema: "core",
                table: "etap1series",
                column: "tier2_ranking_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_etap1series_distinct_rankings",
                schema: "core",
                table: "etap1series",
                sql: "tier1_ranking_id <> tier2_ranking_id");

            migrationBuilder.CreateIndex(
                name: "IX_editions_tier1_ranking_total_id",
                schema: "core",
                table: "editions",
                column: "tier1_ranking_total_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_editions_tier2_ranking_total_id",
                schema: "core",
                table: "editions",
                column: "tier2_ranking_total_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_editions_distinct_total_rankings",
                schema: "core",
                table: "editions",
                sql: "tier1_ranking_total_id <> tier2_ranking_total_id");

            migrationBuilder.AddForeignKey(
                name: "FK_editions_rankings_tier1_ranking_total_id",
                schema: "core",
                table: "editions",
                column: "tier1_ranking_total_id",
                principalSchema: "core",
                principalTable: "rankings",
                principalColumn: "ranking_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_editions_rankings_tier2_ranking_total_id",
                schema: "core",
                table: "editions",
                column: "tier2_ranking_total_id",
                principalSchema: "core",
                principalTable: "rankings",
                principalColumn: "ranking_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_etap1series_rankings_tier1_ranking_id",
                schema: "core",
                table: "etap1series",
                column: "tier1_ranking_id",
                principalSchema: "core",
                principalTable: "rankings",
                principalColumn: "ranking_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_etap1series_rankings_tier2_ranking_id",
                schema: "core",
                table: "etap1series",
                column: "tier2_ranking_id",
                principalSchema: "core",
                principalTable: "rankings",
                principalColumn: "ranking_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_editions_rankings_tier1_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropForeignKey(
                name: "FK_editions_rankings_tier2_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropForeignKey(
                name: "FK_etap1series_rankings_tier1_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropForeignKey(
                name: "FK_etap1series_rankings_tier2_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropIndex(
                name: "IX_etap1series_tier1_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropIndex(
                name: "IX_etap1series_tier2_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_etap1series_distinct_rankings",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropIndex(
                name: "IX_editions_tier1_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropIndex(
                name: "IX_editions_tier2_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_editions_distinct_total_rankings",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "has_qualified_to_next_etap",
                schema: "core",
                table: "ranking_datas");

            migrationBuilder.DropColumn(
                name: "tier1_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropColumn(
                name: "tier2_ranking_id",
                schema: "core",
                table: "etap1series");

            migrationBuilder.DropColumn(
                name: "are_rankings_public",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "tier1_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "tier2_ranking_total_id",
                schema: "core",
                table: "editions");

            migrationBuilder.RenameColumn(
                name: "are_rankings_public",
                schema: "core",
                table: "etap1series",
                newName: "ranking_published");

            migrationBuilder.AddColumn<Guid>(
                name: "Etap1SeriesId",
                schema: "core",
                table: "rankings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "edition_id",
                schema: "core",
                table: "rankings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                schema: "core",
                table: "rankings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                schema: "core",
                table: "rankings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_rankings_edition_id",
                schema: "core",
                table: "rankings",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_rankings_Etap1SeriesId",
                schema: "core",
                table: "rankings",
                column: "Etap1SeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_rankings_editions_edition_id",
                schema: "core",
                table: "rankings",
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
        }
    }
}
