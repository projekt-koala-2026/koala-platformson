using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUserTeamPrivateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_teams",
                schema: "account",
                table: "user_teams");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_teams",
                schema: "account",
                table: "user_teams",
                columns: new[] { "user_id", "team_id", "position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_teams",
                schema: "account",
                table: "user_teams");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_teams",
                schema: "account",
                table: "user_teams",
                columns: new[] { "user_id", "team_id" });
        }
    }
}
