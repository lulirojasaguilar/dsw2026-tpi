using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSlotConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Doctors",
                newName: "Deleted");

            migrationBuilder.RenameColumn(
                name: "SlotDate",
                table: "AvailabilitySlot",
                newName: "Date");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "AvailabilitySlot",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<short>(
                name: "Year",
                table: "AvailabilityRule",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "Month",
                table: "AvailabilityRule",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "DayOfWeek",
                table: "AvailabilityRule",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "Date", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_DoctorId_Date_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "DoctorId", "Date", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule",
                columns: new[] { "DoctorId", "Year", "Month", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRule_Doctors_DoctorId",
                table: "AvailabilityRule",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors",
                column: "SpecialityId",
                principalTable: "Specialities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRule_Doctors_DoctorId",
                table: "AvailabilityRule");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_DoctorId_Date_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRule");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "Doctors",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "AvailabilitySlot",
                newName: "SlotDate");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "Year",
                table: "AvailabilityRule",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Month",
                table: "AvailabilityRule",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "AvailabilityRule",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors",
                column: "SpecialityId",
                principalTable: "Specialities",
                principalColumn: "Id");
        }
    }
}
