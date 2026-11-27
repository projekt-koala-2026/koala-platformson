using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAllTaskTabelesAndAddedTaskFileToEdition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_answers_tasks_task_item_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_task_submissions_tasks_task_id",
                schema: "core",
                table: "task_submissions");

            migrationBuilder.DropColumn(
                name: "content_json",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "image_id",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "image_type",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "tier",
                schema: "core",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "task_id",
                schema: "core",
                table: "task_submissions",
                newName: "task_part_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_submissions_task_id",
                schema: "core",
                table: "task_submissions",
                newName: "IX_task_submissions_task_part_id");

            migrationBuilder.RenameColumn(
                name: "task_item_id",
                schema: "core",
                table: "task_answers",
                newName: "task_part_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_answers_task_item_id",
                schema: "core",
                table: "task_answers",
                newName: "IX_task_answers_task_part_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_school_task",
                schema: "core",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_secondary_school_task",
                schema: "core",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "task_file_close",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "task_file_extension",
                schema: "core",
                table: "editions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "task_file_id",
                schema: "core",
                table: "editions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "task_file_open",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "task_parts",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    max_submission_length = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_parts_tasks_task_item_id",
                        column: x => x.task_item_id,
                        principalSchema: "core",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_parts_task_item_id",
                schema: "core",
                table: "task_parts",
                column: "task_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_answers_task_parts_task_part_id",
                schema: "core",
                table: "task_answers",
                column: "task_part_id",
                principalSchema: "core",
                principalTable: "task_parts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_submissions_task_parts_task_part_id",
                schema: "core",
                table: "task_submissions",
                column: "task_part_id",
                principalSchema: "core",
                principalTable: "task_parts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_answers_task_parts_task_part_id",
                schema: "core",
                table: "task_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_task_submissions_task_parts_task_part_id",
                schema: "core",
                table: "task_submissions");

            migrationBuilder.DropTable(
                name: "task_parts",
                schema: "core");

            migrationBuilder.DropColumn(
                name: "is_primary_school_task",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "is_secondary_school_task",
                schema: "core",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "task_file_close",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "task_file_extension",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "task_file_id",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "task_file_open",
                schema: "core",
                table: "editions");

            migrationBuilder.RenameColumn(
                name: "task_part_id",
                schema: "core",
                table: "task_submissions",
                newName: "task_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_submissions_task_part_id",
                schema: "core",
                table: "task_submissions",
                newName: "IX_task_submissions_task_id");

            migrationBuilder.RenameColumn(
                name: "task_part_id",
                schema: "core",
                table: "task_answers",
                newName: "task_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_answers_task_part_id",
                schema: "core",
                table: "task_answers",
                newName: "IX_task_answers_task_item_id");

            migrationBuilder.AddColumn<string>(
                name: "content_json",
                schema: "core",
                table: "tasks",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "image_id",
                schema: "core",
                table: "tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_type",
                schema: "core",
                table: "tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                schema: "core",
                table: "tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

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
                name: "FK_task_submissions_tasks_task_id",
                schema: "core",
                table: "task_submissions",
                column: "task_id",
                principalSchema: "core",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
