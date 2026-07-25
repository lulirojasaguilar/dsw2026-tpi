using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class UpdateAvailabilityConstraintsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_Doctors_DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilitySlot_DoctorId_SlotDate_StartTime",
                table: "AvailabilitySlot",
                newName: "UX_AvailabilitySlot_Doctor_Date_StartTime");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule",
                newName: "UX_AvailabilityRule_ActiveRule");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "AvailabilitySlot",
                type: "time(0)",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "AvailabilitySlot",
                type: "time(0)",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "AvailabilityRule",
                type: "time(0)",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "AvailabilityRule",
                type: "time(0)",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AvailabilityRule_Id_DoctorId",
                table: "AvailabilityRule",
                columns: new[] { "Id", "DoctorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "DoctorId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilitySlot_Status",
                table: "AvailabilitySlot",
                sql: "[Status] IN ('AVAILABLE', 'BOOKED', 'BLOCKED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilitySlot_TimeRange",
                table: "AvailabilitySlot",
                sql: "[StartTime] < [EndTime]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilityRule_DayOfWeek",
                table: "AvailabilityRule",
                sql: "[DayOfWeek] BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilityRule_Month",
                table: "AvailabilityRule",
                sql: "[Month] BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilityRule_TimeRange",
                table: "AvailabilityRule",
                sql: "[StartTime] < [EndTime]");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "DoctorId" },
                principalTable: "AvailabilityRule",
                principalColumns: new[] { "Id", "DoctorId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilitySlot_Status",
                table: "AvailabilitySlot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilitySlot_TimeRange",
                table: "AvailabilitySlot");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AvailabilityRule_Id_DoctorId",
                table: "AvailabilityRule");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilityRule_DayOfWeek",
                table: "AvailabilityRule");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilityRule_Month",
                table: "AvailabilityRule");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilityRule_TimeRange",
                table: "AvailabilityRule");

            migrationBuilder.RenameIndex(
                name: "UX_AvailabilitySlot_Doctor_Date_StartTime",
                table: "AvailabilitySlot",
                newName: "IX_AvailabilitySlot_DoctorId_SlotDate_StartTime");

            migrationBuilder.RenameIndex(
                name: "UX_AvailabilityRule_ActiveRule",
                table: "AvailabilityRule",
                newName: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "AvailabilitySlot",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(0)");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "AvailabilitySlot",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(0)");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "AvailabilityRule",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(0)");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "AvailabilityRule",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(0)");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId",
                principalTable: "AvailabilityRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_Doctors_DoctorId",
                table: "AvailabilitySlot",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
