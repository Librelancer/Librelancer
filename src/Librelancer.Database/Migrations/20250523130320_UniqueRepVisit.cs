using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibreLancer.Database.Migrations
{
    /// <inheritdoc />
    public partial class UniqueRepVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolarNickname",
                table: "VisitEntry");

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "VisitEntry",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "Hash",
                table: "VisitEntry",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "RepGroup",
                table: "Reputation",
                type: "TEXT COLLATE NOCASE",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "Reputation",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitEntry_CharacterId_Hash",
                table: "VisitEntry",
                columns: new[] { "CharacterId", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reputation_CharacterId_RepGroup",
                table: "Reputation",
                columns: new[] { "CharacterId", "RepGroup" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitEntry_CharacterId_Hash",
                table: "VisitEntry");

            migrationBuilder.DropIndex(
                name: "IX_Reputation_CharacterId_RepGroup",
                table: "Reputation");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "VisitEntry");

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "VisitEntry",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "SolarNickname",
                table: "VisitEntry",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RepGroup",
                table: "Reputation",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT COLLATE NOCASE",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "Reputation",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
