using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCompetitionResourceMenagment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rodos_members_user_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropForeignKey(
                name: "FK_rodos_teams_team_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropTable(
                name: "members",
                schema: "account");

            migrationBuilder.DropTable(
                name: "user_teams",
                schema: "account");

            migrationBuilder.DropIndex(
                name: "IX_rodos_user_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropColumn(
                name: "active",
                schema: "account",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropColumn(
                name: "active",
                schema: "account",
                table: "links");

            migrationBuilder.RenameColumn(
                name: "member_id",
                schema: "account",
                table: "team_members",
                newName: "rodo_id");

            migrationBuilder.RenameIndex(
                name: "IX_team_members_member_id",
                schema: "account",
                table: "team_members",
                newName: "IX_team_members_rodo_id");

            migrationBuilder.RenameColumn(
                name: "team_id",
                schema: "account",
                table: "rodos",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_rodos_team_id",
                schema: "account",
                table: "rodos",
                newName: "IX_rodos_TeamId");

            migrationBuilder.AddColumn<bool>(
                name: "declared_at_least_13",
                schema: "account",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "declared_at_least_18",
                schema: "account",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "account",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "captain_id",
                schema: "account",
                table: "teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "guardian_id",
                schema: "account",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                schema: "account",
                table: "teams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "teams",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "account",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "TeamJoinCodes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "account",
                table: "TeamJoinCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "account",
                table: "team_members",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "name_first",
                schema: "account",
                table: "team_members",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_last",
                schema: "account",
                table: "team_members",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rodo_state",
                schema: "account",
                table: "team_members",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "team_members",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "account",
                table: "team_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "account",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "account",
                table: "rodos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "account",
                table: "links",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "account",
                table: "links",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-1f8e68687e51"),
                column: "name",
                value: "TEAM_GUARDIAN");

            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-2068e28b6cf3"),
                column: "name",
                value: "TEAM_CAPTAIN");

            migrationBuilder.CreateIndex(
                name: "IX_teams_captain_id",
                schema: "account",
                table: "teams",
                column: "captain_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_guardian_id",
                schema: "account",
                table: "teams",
                column: "guardian_id");

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_teams_TeamId",
                schema: "account",
                table: "rodos",
                column: "TeamId",
                principalSchema: "account",
                principalTable: "teams",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members",
                column: "rodo_id",
                principalSchema: "account",
                principalTable: "rodos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_users_captain_id",
                schema: "account",
                table: "teams",
                column: "captain_id",
                principalSchema: "account",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_users_guardian_id",
                schema: "account",
                table: "teams",
                column: "guardian_id",
                principalSchema: "account",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rodos_teams_TeamId",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_users_captain_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_users_guardian_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_captain_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_guardian_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "declared_at_least_13",
                schema: "account",
                table: "users");

            migrationBuilder.DropColumn(
                name: "declared_at_least_18",
                schema: "account",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "users");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "account",
                table: "users");

            migrationBuilder.DropColumn(
                name: "captain_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "guardian_id",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "tier",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "account",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "TeamJoinCodes");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "account",
                table: "TeamJoinCodes");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "name_first",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "name_last",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "rodo_state",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "account",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "account",
                table: "links");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "account",
                table: "links");

            migrationBuilder.RenameColumn(
                name: "rodo_id",
                schema: "account",
                table: "team_members",
                newName: "member_id");

            migrationBuilder.RenameIndex(
                name: "IX_team_members_rodo_id",
                schema: "account",
                table: "team_members",
                newName: "IX_team_members_member_id");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                schema: "account",
                table: "rodos",
                newName: "team_id");

            migrationBuilder.RenameIndex(
                name: "IX_rodos_TeamId",
                schema: "account",
                table: "rodos",
                newName: "IX_rodos_team_id");

            migrationBuilder.AddColumn<bool>(
                name: "active",
                schema: "account",
                table: "sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "team_id",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "account",
                table: "rodos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "active",
                schema: "account",
                table: "links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "members",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_first = table.Column<string>(type: "text", nullable: false),
                    name_last = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_teams",
                schema: "account",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_teams", x => new { x.user_id, x.team_id, x.position });
                    table.ForeignKey(
                        name: "FK_user_teams_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "account",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_teams_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "account",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-1f8e68687e51"),
                column: "name",
                value: "TEAM_ADMIN");

            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-2068e28b6cf3"),
                column: "name",
                value: "TEAM_PLAYER");

            migrationBuilder.CreateIndex(
                name: "IX_rodos_user_id",
                schema: "account",
                table: "rodos",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_teams_team_id",
                schema: "account",
                table: "user_teams",
                column: "team_id");

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_members_user_id",
                schema: "account",
                table: "rodos",
                column: "user_id",
                principalSchema: "account",
                principalTable: "members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_teams_team_id",
                schema: "account",
                table: "rodos",
                column: "team_id",
                principalSchema: "account",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members",
                column: "member_id",
                principalSchema: "account",
                principalTable: "members",
                principalColumn: "id");
        }
    }
}
