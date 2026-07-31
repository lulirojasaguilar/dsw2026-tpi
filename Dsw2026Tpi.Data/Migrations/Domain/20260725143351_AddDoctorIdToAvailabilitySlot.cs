using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class AddDoctorIdToAvailabilitySlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "AvailabilitySlot",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE slot
                SET slot.DoctorId = availabilityRule.DoctorId
                FROM AvailabilitySlot AS slot
                INNER JOIN AvailabilityRule AS availabilityRule
                ON slot.AvailabilityRuleId = availabilityRule.Id;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                table: "AvailabilitySlot",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot",
                column: "AvailabilityRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_DoctorId_SlotDate_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "DoctorId", "SlotDate", "StartTime" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_Doctors_DoctorId",
                table: "AvailabilitySlot",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_Doctors_DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlot_DoctorId_SlotDate_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "SlotDate", "StartTime" },
                unique: true,
                filter: "[Deleted] = 0");
        }
    }
}
