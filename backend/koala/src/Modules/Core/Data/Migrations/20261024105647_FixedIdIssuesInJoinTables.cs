using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedIdIssuesInJoinTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Etap1Series_editions_edition_id",
                schema: "core",
                table: "Etap1Series");

            migrationBuilder.DropIndex(
                name: "IX_Etap1Series_edition_id",
                schema: "core",
                table: "Etap1Series");

            migrationBuilder.DropColumn(
                name: "edition_id",
                schema: "core",
                table: "Etap1Series");

            migrationBuilder.RenameColumn(
                name: "data_start",
                schema: "core",
                table: "Etap1Series",
                newName: "date_start");

            migrationBuilder.RenameColumn(
                name: "data_end",
                schema: "core",
                table: "Etap1Series",
                newName: "date_end");

            migrationBuilder.CreateTable(
                name: "edition_Etap1Series",
                schema: "core",
                columns: table => new
                {
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Etap1Series_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_Etap1Series", x => new { x.edition_id, x.Etap1Series_id });
                    table.ForeignKey(
                        name: "FK_edition_Etap1Series_editions_edition_id",
                        column: x => x.edition_id,
                        principalSchema: "core",
                        principalTable: "editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_edition_Etap1Series_Etap1Series_Etap1Series_id",
                        column: x => x.Etap1Series_id,
                        principalSchema: "core",
                        principalTable: "Etap1Series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_corrs",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correct_answer = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_corrs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Etap1Series_tasks",
                schema: "core",
                columns: table => new
                {
                    Etap1Series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etap1Series_tasks", x => new { x.Etap1Series_id, x.task_id });
                    table.ForeignKey(
                        name: "FK_Etap1Series_tasks_Etap1Series_Etap1Series_id",
                        column: x => x.Etap1Series_id,
                        principalSchema: "core",
                        principalTable: "Etap1Series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Etap1Series_tasks_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "core",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_item_task_corrs",
                schema: "core",
                columns: table => new
                {
                    task_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_corr_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_item_task_corrs", x => new { x.task_item_id, x.task_corr_id });
                    table.ForeignKey(
                        name: "FK_task_item_task_corrs_task_corrs_task_corr_id",
                        column: x => x.task_corr_id,
                        principalSchema: "core",
                        principalTable: "task_corrs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_item_task_corrs_tasks_task_item_id",
                        column: x => x.task_item_id,
                        principalSchema: "core",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edition_Etap1Series_Etap1Series_id",
                schema: "core",
                table: "edition_Etap1Series",
                column: "Etap1Series_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etap1Series_tasks_task_id",
                schema: "core",
                table: "Etap1Series_tasks",
                column: "task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_item_task_corrs_task_corr_id",
                schema: "core",
                table: "task_item_task_corrs",
                column: "task_corr_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edition_Etap1Series",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Etap1Series_tasks",
                schema: "core");

            migrationBuilder.DropTable(
                name: "task_item_task_corrs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "task_corrs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "core");

            migrationBuilder.RenameColumn(
                name: "date_start",
                schema: "core",
                table: "Etap1Series",
                newName: "data_start");

            migrationBuilder.RenameColumn(
                name: "date_end",
                schema: "core",
                table: "Etap1Series",
                newName: "data_end");

            migrationBuilder.AddColumn<Guid>(
                name: "edition_id",
                schema: "core",
                table: "Etap1Series",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Etap1Series_edition_id",
                schema: "core",
                table: "Etap1Series",
                column: "edition_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Etap1Series_editions_edition_id",
                schema: "core",
                table: "Etap1Series",
                column: "edition_id",
                principalSchema: "core",
                principalTable: "editions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
