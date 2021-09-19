using Microsoft.EntityFrameworkCore.Migrations;

namespace LibreLancer.Database.Migrations
{
    public partial class PlayerSpacePosition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "X",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Y",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Z",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "X",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Z",
                table: "Characters");
        }
    }
}
