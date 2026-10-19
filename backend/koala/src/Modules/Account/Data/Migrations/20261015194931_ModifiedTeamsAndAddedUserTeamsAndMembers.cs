using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedTeamsAndAddedUserTeamsAndMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_members_users_user_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "account",
                table: "team_members");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "account",
                table: "team_members",
                newName: "member_id");

            migrationBuilder.CreateTable(
                name: "members",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_first = table.Column<string>(type: "text", nullable: false),
                    name_last = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<string>(type: "text", nullable: false)
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
                    table.PrimaryKey("PK_user_teams", x => new { x.user_id, x.team_id });
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

            migrationBuilder.CreateIndex(
                name: "IX_team_members_member_id",
                schema: "account",
                table: "team_members",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_teams_team_id",
                schema: "account",
                table: "user_teams",
                column: "team_id");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members",
                column: "member_id",
                principalSchema: "account",
                principalTable: "members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_team_members_member_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.RenameColumn(
                name: "member_id",
                schema: "account",
                table: "team_members",
                newName: "user_id");

            migrationBuilder.AddColumn<string>(
                name: "position",
                schema: "account",
                table: "team_members",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_users_user_id",
                schema: "account",
                table: "team_members",
                column: "user_id",
                principalSchema: "account",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
