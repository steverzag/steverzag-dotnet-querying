using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpDataWorkerBeta.Migrations
{
    /// <inheritdoc />
    public partial class FixJobAndTaxInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "Persons");

            migrationBuilder.AlterColumn<double>(
                name: "TaxRate",
                table: "TaxInfos",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<double>(
                name: "MonthlySalary",
                table: "Jobs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "Jobs");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "TaxInfos",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<double>(
                name: "MonthlySalary",
                table: "Persons",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
