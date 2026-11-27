using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRodoModerator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "account",
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[] { new Guid("01a027c5-d599-73de-bd5c-2597ff8e14e5"), "ORGANIZATION_RODO_MODERATOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-2597ff8e14e5"));
        }
    }
}
