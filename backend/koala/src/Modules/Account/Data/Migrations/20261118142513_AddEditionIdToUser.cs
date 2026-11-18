using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rodos_teams_TeamId",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropIndex(
                name: "IX_rodos_TeamId",
                schema: "account",
                table: "rodos");

            migrationBuilder.DropColumn(
                name: "TeamId",
                schema: "account",
                table: "rodos");

            migrationBuilder.AddColumn<Guid>(
                name: "edition_id",
                schema: "account",
                table: "users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "edition_id",
                schema: "account",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rodos_TeamId",
                schema: "account",
                table: "rodos",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_teams_TeamId",
                schema: "account",
                table: "rodos",
                column: "TeamId",
                principalSchema: "account",
                principalTable: "teams",
                principalColumn: "id");
        }
    }
}
