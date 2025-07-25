using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibreLancer.Database.Migrations
{
    /// <inheritdoc />
    public partial class VisitHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitHistoryEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Hash = table.Column<uint>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<double>(type: "REAL", nullable: false),
                    UpdateDate = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitHistoryEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitHistoryEntry_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitHistoryEntry_CharacterId",
                table: "VisitHistoryEntry",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitHistoryEntry_CharacterId_Kind_Hash",
                table: "VisitHistoryEntry",
                columns: new[] { "CharacterId", "Kind", "Hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitHistoryEntry");
        }
    }
}
