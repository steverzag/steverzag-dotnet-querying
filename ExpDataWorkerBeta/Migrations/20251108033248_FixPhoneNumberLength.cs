using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpDataWorkerBeta.Migrations
{
    /// <inheritdoc />
    public partial class FixPhoneNumberLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Phones",
                type: "char(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(15)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Phones",
                type: "char(15)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(20)");
        }
    }
}
