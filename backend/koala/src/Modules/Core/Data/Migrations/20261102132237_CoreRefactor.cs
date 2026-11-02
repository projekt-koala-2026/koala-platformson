using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoreRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_answers_tasks_task_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropTable(
                name: "edition_subeditions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "subedition_tasks",
                schema: "core");

            migrationBuilder.DropTable(
                name: "task_item_task_corrs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "task_corrs",
                schema: "core");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_answers",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "auto_review_state",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "review_1_state",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "review_2_state",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "reviewer_1_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "reviewer_2_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.RenameColumn(
                name: "version",
                schema: "core",
                table: "task_answers",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "team_answer",
                schema: "core",
                table: "task_answers",
                newName: "answer");

            migrationBuilder.RenameColumn(
                name: "task_id",
                schema: "core",
                table: "task_answers",
                newName: "task_item_id");

            migrationBuilder.RenameColumn(
                name: "team_id",
                schema: "core",
                table: "task_answers",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_task_answers_task_id",
                schema: "core",
                table: "task_answers",
                newName: "IX_task_answers_task_item_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "core",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "subedition_id",
                schema: "core",
                table: "tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "type",
                schema: "core",
                table: "tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                schema: "core",
                table: "tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "core",
                table: "task_answers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "task_answers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "answer_close",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "answer_open",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "edition_id",
                schema: "core",
                table: "subeditions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "core",
                table: "subeditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_answers",
                schema: "core",
                table: "task_answers",
                column: "id");

            migrationBuilder.CreateTable(
                name: "task_submissions",
                schema: "core",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_1_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewer_2_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_review_state = table.Column<bool>(type: "boolean", nullable: true),
                    review_1_state = table.Column<bool>(type: "boolean", nullable: true),
                    review_2_state = table.Column<bool>(type: "boolean", nullable: true),
                    team_answer = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_submissions", x => new { x.team_id, x.task_id });
                    table.ForeignKey(
                        name: "FK_task_submissions_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "core",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_subedition_id",
                schema: "core",
                table: "tasks",
                column: "subedition_id");

            migrationBuilder.CreateIndex(
                name: "IX_subeditions_edition_id",
                schema: "core",
                table: "subeditions",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_submissions_task_id",
                schema: "core",
                table: "task_submissions",
                column: "task_id");

            migrationBuilder.AddForeignKey(
                name: "FK_subeditions_editions_edition_id",
                schema: "core",
                table: "subeditions",
                column: "edition_id",
                principalSchema: "core",
                principalTable: "editions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_answers_tasks_task_item_id",
                schema: "core",
                table: "task_answers",
                column: "task_item_id",
                principalSchema: "core",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_subeditions_subedition_id",
                schema: "core",
                table: "tasks",
                column: "subedition_id",
                principalSchema: "core",
                principalTable: "subeditions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subeditions_editions_edition_id",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropForeignKey(
                name: "FK_task_answers_tasks_task_item_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_subeditions_subedition_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "task_submissions",
                schema: "core");

            migrationBuilder.DropIndex(
                name: "IX_tasks_subedition_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_answers",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropIndex(
                name: "IX_subeditions_edition_id",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "subedition_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropColumn(
                name: "answer_close",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "answer_open",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "edition_id",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "core",
                table: "subeditions");

            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "core",
                table: "task_answers",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "task_item_id",
                schema: "core",
                table: "task_answers",
                newName: "task_id");

            migrationBuilder.RenameColumn(
                name: "answer",
                schema: "core",
                table: "task_answers",
                newName: "team_answer");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "core",
                table: "task_answers",
                newName: "team_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_answers_task_item_id",
                schema: "core",
                table: "task_answers",
                newName: "IX_task_answers_task_id");

            migrationBuilder.AddColumn<bool>(
                name: "auto_review_state",
                schema: "core",
                table: "task_answers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "review_1_state",
                schema: "core",
                table: "task_answers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "review_2_state",
                schema: "core",
                table: "task_answers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewer_1_id",
                schema: "core",
                table: "task_answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewer_2_id",
                schema: "core",
                table: "task_answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_answers",
                schema: "core",
                table: "task_answers",
                columns: new[] { "team_id", "task_id" });

            migrationBuilder.CreateTable(
                name: "edition_subeditions",
                schema: "core",
                columns: table => new
                {
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subedition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edition_subeditions", x => new { x.edition_id, x.subedition_id });
                    table.ForeignKey(
                        name: "FK_edition_subeditions_editions_edition_id",
                        column: x => x.edition_id,
                        principalSchema: "core",
                        principalTable: "editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_edition_subeditions_subeditions_subedition_id",
                        column: x => x.subedition_id,
                        principalSchema: "core",
                        principalTable: "subeditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subedition_tasks",
                schema: "core",
                columns: table => new
                {
                    subedition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subedition_tasks", x => new { x.subedition_id, x.task_id });
                    table.ForeignKey(
                        name: "FK_subedition_tasks_subeditions_subedition_id",
                        column: x => x.subedition_id,
                        principalSchema: "core",
                        principalTable: "subeditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subedition_tasks_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "core",
                        principalTable: "tasks",
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
                name: "IX_edition_subeditions_subedition_id",
                schema: "core",
                table: "edition_subeditions",
                column: "subedition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subedition_tasks_task_id",
                schema: "core",
                table: "subedition_tasks",
                column: "task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_item_task_corrs_task_corr_id",
                schema: "core",
                table: "task_item_task_corrs",
                column: "task_corr_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_task_answers_tasks_task_id",
                schema: "core",
                table: "task_answers",
                column: "task_id",
                principalSchema: "core",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
