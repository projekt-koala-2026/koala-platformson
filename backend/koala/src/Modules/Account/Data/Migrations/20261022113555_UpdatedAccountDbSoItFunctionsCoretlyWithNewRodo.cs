using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAccountDbSoItFunctionsCoretlyWithNewRodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rodos_users_user_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_members_user_id",
                schema: "account",
                table: "rodos",
                column: "user_id",
                principalSchema: "account",
                principalTable: "members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rodos_members_user_id",
                schema: "account",
                table: "rodos");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "account",
                table: "rodos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_rodos_users_user_id",
                schema: "account",
                table: "rodos",
                column: "user_id",
                principalSchema: "account",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
