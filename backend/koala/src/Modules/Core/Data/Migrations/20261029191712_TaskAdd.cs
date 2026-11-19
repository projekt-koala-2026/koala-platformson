using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaskAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_answers",
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
                    table.PrimaryKey("PK_task_answers", x => new { x.team_id, x.task_id });
                    table.ForeignKey(
                        name: "FK_task_answers_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "core",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_answers_task_id",
                schema: "core",
                table: "task_answers",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_answers",
                schema: "core");
        }
    }
}
