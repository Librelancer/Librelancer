using Microsoft.EntityFrameworkCore.Migrations;

namespace LibreLancer.Database.Migrations
{
    public partial class AddOrientation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "RotationW",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AddColumn<float>(
                name: "RotationX",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RotationY",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RotationZ",
                table: "Characters",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RotationW",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RotationX",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RotationY",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RotationZ",
                table: "Characters");
        }
    }
}
