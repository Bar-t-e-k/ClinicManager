using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationDosage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dosage",
                table: "VisitMedications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dosage",
                table: "VisitMedications");
        }
    }
}
