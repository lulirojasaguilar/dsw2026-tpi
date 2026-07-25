using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class UpdateAvailabilityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "AvailabilitySlot",
                newName: "SlotDate");

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilitySlot",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilityRule",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "SlotDate", "StartTime" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule",
                columns: new[] { "DoctorId", "Year", "Month", "DayOfWeek", "StartTime", "EndTime" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId",
                principalTable: "AvailabilityRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule");

            migrationBuilder.RenameColumn(
                name: "SlotDate",
                table: "AvailabilitySlot",
                newName: "Date");

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilitySlot",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "Deleted",
                table: "AvailabilityRule",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "Date", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule",
                columns: new[] { "DoctorId", "Year", "Month", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId",
                principalTable: "AvailabilityRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
