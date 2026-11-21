using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewRankingSystemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rankings_subeditions_subedition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rankings",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "points",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "points_max",
                schema: "core",
                table: "rankings");

            migrationBuilder.RenameColumn(
                name: "subedition_id",
                schema: "core",
                table: "rankings",
                newName: "SubEditionId");

            migrationBuilder.RenameColumn(
                name: "team_name",
                schema: "core",
                table: "rankings",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "core",
                table: "rankings",
                newName: "edition_id");

            migrationBuilder.RenameIndex(
                name: "IX_rankings_subedition_id",
                schema: "core",
                table: "rankings",
                newName: "IX_rankings_SubEditionId");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubEditionId",
                schema: "core",
                table: "rankings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ranking_id",
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
                name: "PK_rankings",
                schema: "core",
                table: "rankings",
                column: "ranking_id");

            migrationBuilder.CreateTable(
                name: "ranking_datas",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ranking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "text", nullable: false),
                    points_achived = table.Column<int>(type: "integer", nullable: false),
                    points_max = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_datas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ranking_datas_rankings_ranking_id",
                        column: x => x.ranking_id,
                        principalSchema: "core",
                        principalTable: "rankings",
                        principalColumn: "ranking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "winners",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    EditionId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_rankings_edition_id",
                schema: "core",
                table: "rankings",
                column: "edition_id");

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
                name: "IX_ranking_datas_ranking_id",
                schema: "core",
                table: "ranking_datas",
                column: "ranking_id");

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
                name: "FK_rankings_editions_edition_id",
                schema: "core",
                table: "rankings",
                column: "edition_id",
                principalSchema: "core",
                principalTable: "editions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rankings_subeditions_SubEditionId",
                schema: "core",
                table: "rankings",
                column: "SubEditionId",
                principalSchema: "core",
                principalTable: "subeditions",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "FK_rankings_editions_edition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropForeignKey(
                name: "FK_rankings_subeditions_SubEditionId",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropTable(
                name: "ranking_datas",
                schema: "core");

            migrationBuilder.DropTable(
                name: "winners",
                schema: "core");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rankings",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropIndex(
                name: "IX_rankings_edition_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropIndex(
                name: "IX_editions_winner_tier1_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropIndex(
                name: "IX_editions_winner_tier2_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "ranking_id",
                schema: "core",
                table: "rankings");

            migrationBuilder.DropColumn(
                name: "is_public",
                schema: "core",
                table: "rankings");

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

            migrationBuilder.RenameColumn(
                name: "SubEditionId",
                schema: "core",
                table: "rankings",
                newName: "subedition_id");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "core",
                table: "rankings",
                newName: "team_name");

            migrationBuilder.RenameColumn(
                name: "edition_id",
                schema: "core",
                table: "rankings",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_rankings_SubEditionId",
                schema: "core",
                table: "rankings",
                newName: "IX_rankings_subedition_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "subedition_id",
                schema: "core",
                table: "rankings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "points",
                schema: "core",
                table: "rankings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "points_max",
                schema: "core",
                table: "rankings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_rankings",
                schema: "core",
                table: "rankings",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_rankings_subeditions_subedition_id",
                schema: "core",
                table: "rankings",
                column: "subedition_id",
                principalSchema: "core",
                principalTable: "subeditions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
