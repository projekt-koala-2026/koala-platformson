using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCompetitionResourceMenagment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_end",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "core",
                table: "editions");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "core",
                table: "tasks",
                newName: "tier");

            migrationBuilder.RenameColumn(
                name: "team_answer",
                schema: "core",
                table: "task_submissions",
                newName: "submission");

            migrationBuilder.RenameColumn(
                name: "date_start",
                schema: "core",
                table: "subeditions",
                newName: "updated_at");

            // The old UUID version cannot be cast to an integer by PostgreSQL.
            // Existing tasks start a new integer version sequence at 1.
            migrationBuilder.Sql("ALTER TABLE core.tasks ALTER COLUMN version DROP DEFAULT; ALTER TABLE core.tasks ALTER COLUMN version TYPE integer USING 1;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "core",
                table: "task_submissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "core",
                table: "task_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "task_submissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "task_answers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "answer_close",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "core",
                table: "subeditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                schema: "core",
                table: "schools",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "core",
                table: "schools",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "core",
                table: "editions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "team_register_close",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "team_register_open",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "core",
                table: "editions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "core",
                table: "task_submissions");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "core",
                table: "task_submissions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "core",
                table: "task_submissions");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "core",
                table: "subeditions");

            migrationBuilder.DropColumn(
                name: "tier",
                schema: "core",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "core",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "team_register_close",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "team_register_open",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "core",
                table: "editions");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "core",
                table: "editions");

            migrationBuilder.RenameColumn(
                name: "tier",
                schema: "core",
                table: "tasks",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "submission",
                schema: "core",
                table: "task_submissions",
                newName: "team_answer");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "core",
                table: "subeditions",
                newName: "date_start");

            migrationBuilder.AlterColumn<Guid>(
                name: "version",
                schema: "core",
                table: "tasks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "core",
                table: "task_answers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "answer_close",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_end",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                schema: "core",
                table: "subeditions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                schema: "core",
                table: "editions",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
