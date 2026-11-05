using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Cms.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedCorectVersionForStaticPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-abc51fe1d87f"),
                column: "version",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-afa2801aa4a4"),
                column: "version",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b241dc642361"),
                column: "version",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b426015c004a"),
                column: "version",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b86c61973289"),
                column: "version",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-abc51fe1d87f"),
                column: "version",
                value: 0);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-afa2801aa4a4"),
                column: "version",
                value: 0);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b241dc642361"),
                column: "version",
                value: 0);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b426015c004a"),
                column: "version",
                value: 0);

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "static_pages",
                keyColumn: "id",
                keyValue: new Guid("01a065c3-3bec-7224-b7b2-b86c61973289"),
                column: "version",
                value: 0);
        }
    }
}
