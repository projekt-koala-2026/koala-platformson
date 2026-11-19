using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedConcurencyTokenForRodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members",
                column: "rodo_id",
                principalSchema: "account",
                principalTable: "rodos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members");

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_rodos_rodo_id",
                schema: "account",
                table: "team_members",
                column: "rodo_id",
                principalSchema: "account",
                principalTable: "rodos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
