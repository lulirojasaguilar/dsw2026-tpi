using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class FixAppointmentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Appointment_AvailabilityId",
                table: "Appointment");

            migrationBuilder.CreateIndex(
                name: "UX_Appointment_AvailabilityId_Booked",
                table: "Appointment",
                column: "AvailabilityId",
                unique: true,
                filter: "[Status] = 'BOOKED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Appointment_AvailabilityId_Booked",
                table: "Appointment");

            migrationBuilder.CreateIndex(
                name: "UX_Appointment_AvailabilityId",
                table: "Appointment",
                column: "AvailabilityId",
                unique: true);
        }
    }
}
