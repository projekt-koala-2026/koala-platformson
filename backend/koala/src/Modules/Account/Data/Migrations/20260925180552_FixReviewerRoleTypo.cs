using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Account.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewerRoleTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-18c05fc3fa53"),
                column: "name",
                value: "ORGANIZATION_REVIEWER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "account",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("01a027c5-d599-73de-bd5c-18c05fc3fa53"),
                column: "name",
                value: "ORGANIZATION_REVIUER");
        }
    }
}
