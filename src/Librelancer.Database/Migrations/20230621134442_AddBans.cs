using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LibreLancer.Database.Migrations
{
    public partial class AddBans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanExpiry",
                table: "Accounts",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanExpiry",
                table: "Accounts");
        }
    }
}
