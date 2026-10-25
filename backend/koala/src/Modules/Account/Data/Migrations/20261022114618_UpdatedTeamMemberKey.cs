using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTeamMemberKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_members",
                schema: "account",
                table: "team_members");

            migrationBuilder.AlterColumn<Guid>(
                name: "member_id",
                schema: "account",
                table: "team_members",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                schema: "account",
                table: "team_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing rows must not all receive the same zero UUID before this
            // column becomes the primary key.
            migrationBuilder.Sql("""
                UPDATE account.team_members
                SET id = md5(
                    team_id::text || ':' || COALESCE(member_id::text, '') || ':' || ctid::text
                )::uuid;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_members",
                schema: "account",
                table: "team_members",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members",
                column: "member_id",
                principalSchema: "account",
                principalTable: "members",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_members_members_member_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_members",
                schema: "account",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "account",
                table: "team_members");

            migrationBuilder.AlterColumn<Guid>(
                name: "member_id",
                schema: "account",
                table: "team_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_members",
                schema: "account",
                table: "team_members",
                columns: new[] { "member_id", "team_id" });

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
    }
}
