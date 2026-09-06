using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibreLancer.Database.Migrations
{
    /// <inheritdoc />
    public partial class DestroyedParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestroyedParts",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestroyedParts",
                table: "Characters");
        }
    }
}
