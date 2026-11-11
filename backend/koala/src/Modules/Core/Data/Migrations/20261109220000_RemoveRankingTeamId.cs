using System;
using koala.src.Modules.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace koala.src.Modules.Core.Data.Migrations
{
    [DbContext(typeof(CoreDbContext))]
    [Migration("20261109220000_RemoveRankingTeamId")]
    public partial class RemoveRankingTeamId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "team_id", schema: "core", table: "rankings");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "team_id", schema: "core", table: "rankings", type: "uuid", nullable: true);
        }
    }
}
