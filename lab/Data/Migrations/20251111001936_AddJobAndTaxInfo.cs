using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpDataWorkerBeta.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAndTaxInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MonthlySalary",
                table: "Persons",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SinceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartHour = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndHour = table.Column<TimeOnly>(type: "time", nullable: true),
                    NumberOfEmployees = table.Column<short>(type: "smallint", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastDeclarationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastDeclarationAmmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedNextDeclarationAmmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxInfos_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PersonId",
                table: "Jobs",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxInfos_PersonId",
                table: "TaxInfos",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "TaxInfos");

            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "Persons");
        }
    }
}
