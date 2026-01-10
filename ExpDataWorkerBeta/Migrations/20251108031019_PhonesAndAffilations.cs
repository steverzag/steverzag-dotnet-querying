using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpDataWorkerBeta.Migrations
{
    /// <inheritdoc />
    public partial class PhonesAndAffilations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "MaritalStatus",
                table: "Persons",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Affilations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    NumberOfMembers = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affilations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Phones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "char(10)", nullable: true),
                    Number = table.Column<string>(type: "char(15)", nullable: false),
                    IsOptedIn = table.Column<bool>(type: "bit", nullable: true),
                    PhoneType = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Phones_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffiliationPersonMaps",
                columns: table => new
                {
                    AffiliationId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    AffiliationType = table.Column<int>(type: "int", nullable: false),
                    AffiliatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliationPersonMaps", x => new { x.AffiliationId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_AffiliationPersonMaps_Affilations_AffiliationId",
                        column: x => x.AffiliationId,
                        principalTable: "Affilations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffiliationPersonMaps_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkingHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AffiliationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingHours_Affilations_AffiliationId",
                        column: x => x.AffiliationId,
                        principalTable: "Affilations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliationPersonMaps_PersonId",
                table: "AffiliationPersonMaps",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Phones_PersonId",
                table: "Phones",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_AffiliationId",
                table: "WorkingHours",
                column: "AffiliationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliationPersonMaps");

            migrationBuilder.DropTable(
                name: "Phones");

            migrationBuilder.DropTable(
                name: "WorkingHours");

            migrationBuilder.DropTable(
                name: "Affilations");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Persons");
        }
    }
}
