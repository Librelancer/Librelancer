using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LibreLancer.Database.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    AccountIdentifier = table.Column<Guid>(nullable: false),
                    LastLogin = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    Name = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    Rank = table.Column<uint>(nullable: false),
                    Money = table.Column<long>(nullable: false),
                    Voice = table.Column<string>(nullable: true),
                    Costume = table.Column<string>(nullable: true),
                    ComCostume = table.Column<string>(nullable: true),
                    System = table.Column<string>(nullable: true),
                    Base = table.Column<string>(nullable: true),
                    Ship = table.Column<string>(nullable: true),
                    Affiliation = table.Column<string>(nullable: true),
                    AffiliationLocked = table.Column<bool>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    FightersKilled = table.Column<long>(nullable: false),
                    TransportsKilled = table.Column<long>(nullable: false),
                    CapitalKills = table.Column<long>(nullable: false),
                    PlayersKilled = table.Column<long>(nullable: false),
                    MissionsCompleted = table.Column<long>(nullable: false),
                    MissionsFailed = table.Column<long>(nullable: false),
                    AccountId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CargoItem",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    ItemName = table.Column<string>(nullable: true),
                    ItemCount = table.Column<long>(nullable: false),
                    IsMissionItem = table.Column<bool>(nullable: false),
                    CharacterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoItem_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentEntity",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    EquipmentNickname = table.Column<string>(nullable: true),
                    EquipmentHardpoint = table.Column<string>(nullable: true),
                    CharacterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentEntity_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reputation",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    ReputationValue = table.Column<float>(nullable: false),
                    RepGroup = table.Column<string>(nullable: true),
                    CharacterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reputation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reputation_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitEntry",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: true),
                    VisitValue = table.Column<int>(nullable: false),
                    SolarNickname = table.Column<string>(nullable: true),
                    CharacterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitEntry_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountIdentifier",
                table: "Accounts",
                column: "AccountIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_CargoItem_CharacterId",
                table: "CargoItem",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AccountId",
                table: "Characters",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentEntity_CharacterId",
                table: "EquipmentEntity",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reputation_CharacterId",
                table: "Reputation",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitEntry_CharacterId",
                table: "VisitEntry",
                column: "CharacterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CargoItem");

            migrationBuilder.DropTable(
                name: "EquipmentEntity");

            migrationBuilder.DropTable(
                name: "Reputation");

            migrationBuilder.DropTable(
                name: "VisitEntry");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
