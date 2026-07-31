using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations.Domain
{
    /// <inheritdoc />
    public partial class GeneralCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_AvailabilitySlot_AvailabilityId",
                table: "Appointment");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_Patients_PatientId",
                table: "Appointment");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRule_Doctors_DoctorId",
                table: "AvailabilityRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Specialities",
                table: "Specialities");

            migrationBuilder.DropIndex(
                name: "IX_Specialities_Name",
                table: "Specialities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilitySlot",
                table: "AvailabilitySlot");

            migrationBuilder.DropIndex(
                name: "UX_AvailabilitySlot_Doctor_Date_StartTime",
                table: "AvailabilitySlot");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AvailabilityRule_Id_DoctorId",
                table: "AvailabilityRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilityRule",
                table: "AvailabilityRule");

            migrationBuilder.DropIndex(
                name: "UX_AvailabilityRule_ActiveRule",
                table: "AvailabilityRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Appointment",
                table: "Appointment");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AvailabilitySlot");

            migrationBuilder.RenameTable(
                name: "Specialities",
                newName: "Specialties");

            migrationBuilder.RenameTable(
                name: "AvailabilitySlot",
                newName: "AvailabilitySlots");

            migrationBuilder.RenameTable(
                name: "AvailabilityRule",
                newName: "AvailabilityRules");

            migrationBuilder.RenameTable(
                name: "Appointment",
                newName: "Appointments");

            migrationBuilder.RenameIndex(
                name: "IX_Patients_Dni",
                table: "Patients",
                newName: "UX_Patients_Dni");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "Specialties",
                newName: "deleted");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "AvailabilitySlots",
                newName: "deleted");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilitySlot_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlots",
                newName: "IX_AvailabilitySlots_AvailabilityRuleId_DoctorId");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "AvailabilityRules",
                newName: "deleted");

            migrationBuilder.RenameColumn(
                name: "AvailabilityId",
                table: "Appointments",
                newName: "AvailabilitySlotId");

            migrationBuilder.RenameIndex(
                name: "UX_Appointment_AvailabilityId_Booked",
                table: "Appointments",
                newName: "UX_Appointments_AvailabilitySlotId_Booked");

            migrationBuilder.RenameIndex(
                name: "IX_Appointment_PatientId",
                table: "Appointments",
                newName: "IX_Appointments_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_Appointment_DoctorId",
                table: "Appointments",
                newName: "IX_Appointments_DoctorId");

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Patients",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Patients",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "Doctors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql("ALTER TABLE [AvailabilityRules] DROP CONSTRAINT [CK_AvailabilityRule_DayOfWeek];");

            migrationBuilder.AlterColumn<short>(
                name: "DayOfWeek",
                table: "AvailabilityRules",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.Sql("ALTER TABLE [AvailabilityRules] ADD CONSTRAINT [CK_AvailabilityRule_DayOfWeek] CHECK ([DayOfWeek] >= 0 AND [DayOfWeek] <= 6);");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Appointments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Specialties",
                table: "Specialties",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilitySlots",
                table: "AvailabilitySlots",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AvailabilityRules_Id_DoctorId",
                table: "AvailabilityRules",
                columns: new[] { "Id", "DoctorId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilityRules",
                table: "AvailabilityRules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Appointments",
                table: "Appointments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "UX_Patients_ApplicationUserId",
                table: "Patients",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Specialties_Name_Active",
                table: "Specialties",
                column: "Name",
                unique: true,
                filter: "[deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AvailabilitySlots_Doctor_Date_StartTime",
                table: "AvailabilitySlots",
                columns: new[] { "DoctorId", "SlotDate", "StartTime" },
                unique: true,
                filter: "[deleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailabilitySlots_Status",
                table: "AvailabilitySlots",
                sql: "[Status] IN ('AVAILABLE', 'BOOKED', 'BLOCKED')");

            migrationBuilder.CreateIndex(
                name: "UX_AvailabilityRules_ActiveRule",
                table: "AvailabilityRules",
                columns: new[] { "DoctorId", "Year", "Month", "DayOfWeek", "StartTime", "EndTime" },
                unique: true,
                filter: "[deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AvailabilitySlots_AvailabilitySlotId",
                table: "Appointments",
                column: "AvailabilitySlotId",
                principalTable: "AvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Doctors_DoctorId",
                table: "Appointments",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRules_Doctors_DoctorId",
                table: "AvailabilityRules",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityRuleId",
                table: "AvailabilitySlots",
                column: "AvailabilityRuleId",
                principalTable: "AvailabilityRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlots",
                columns: new[] { "AvailabilityRuleId", "DoctorId" },
                principalTable: "AvailabilityRules",
                principalColumns: new[] { "Id", "DoctorId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specialties_SpecialityId",
                table: "Doctors",
                column: "SpecialityId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AvailabilitySlots_AvailabilitySlotId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Doctors_DoctorId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRules_Doctors_DoctorId",
                table: "AvailabilityRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityRuleId",
                table: "AvailabilitySlots");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specialties_SpecialityId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "UX_Patients_ApplicationUserId",
                table: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Specialties",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "UX_Specialties_Name_Active",
                table: "Specialties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilitySlots",
                table: "AvailabilitySlots");

            migrationBuilder.DropIndex(
                name: "UX_AvailabilitySlots_Doctor_Date_StartTime",
                table: "AvailabilitySlots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailabilitySlots_Status",
                table: "AvailabilitySlots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AvailabilityRules_Id_DoctorId",
                table: "AvailabilityRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilityRules",
                table: "AvailabilityRules");

            migrationBuilder.DropIndex(
                name: "UX_AvailabilityRules_ActiveRule",
                table: "AvailabilityRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Appointments",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AttendedAt",
                table: "Appointments");

            migrationBuilder.RenameTable(
                name: "Specialties",
                newName: "Specialities");

            migrationBuilder.RenameTable(
                name: "AvailabilitySlots",
                newName: "AvailabilitySlot");

            migrationBuilder.RenameTable(
                name: "AvailabilityRules",
                newName: "AvailabilityRule");

            migrationBuilder.RenameTable(
                name: "Appointments",
                newName: "Appointment");

            migrationBuilder.RenameIndex(
                name: "UX_Patients_Dni",
                table: "Patients",
                newName: "IX_Patients_Dni");

            migrationBuilder.RenameColumn(
                name: "deleted",
                table: "Specialities",
                newName: "Deleted");

            migrationBuilder.RenameColumn(
                name: "deleted",
                table: "AvailabilitySlot",
                newName: "Deleted");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilitySlots_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot",
                newName: "IX_AvailabilitySlot_AvailabilityRuleId_DoctorId");

            migrationBuilder.RenameColumn(
                name: "deleted",
                table: "AvailabilityRule",
                newName: "Deleted");

            migrationBuilder.RenameColumn(
                name: "AvailabilitySlotId",
                table: "Appointment",
                newName: "AvailabilityId");

            migrationBuilder.RenameIndex(
                name: "UX_Appointments_AvailabilitySlotId_Booked",
                table: "Appointment",
                newName: "UX_Appointment_AvailabilityId_Booked");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointment",
                newName: "IX_Appointment_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointment",
                newName: "IX_Appointment_DoctorId");

            migrationBuilder.AlterColumn<long>(
                name: "Dni",
                table: "Patients",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Patients",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Patients",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "Doctors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AvailabilitySlot",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<byte>(
                name: "DayOfWeek",
                table: "AvailabilityRule",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Appointment",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Specialities",
                table: "Specialities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilitySlot",
                table: "AvailabilitySlot",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AvailabilityRule_Id_DoctorId",
                table: "AvailabilityRule",
                columns: new[] { "Id", "DoctorId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilityRule",
                table: "AvailabilityRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Appointment",
                table: "Appointment",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_Name",
                table: "Specialities",
                column: "Name",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AvailabilitySlot_Doctor_Date_StartTime",
                table: "AvailabilitySlot",
                columns: new[] { "DoctorId", "SlotDate", "StartTime" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AvailabilityRule_ActiveRule",
                table: "AvailabilityRule",
                columns: new[] { "DoctorId", "Year", "Month", "DayOfWeek", "StartTime", "EndTime" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_AvailabilitySlot_AvailabilityId",
                table: "Appointment",
                column: "AvailabilityId",
                principalTable: "AvailabilitySlot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_Patients_PatientId",
                table: "Appointment",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRule_Doctors_DoctorId",
                table: "AvailabilityRule",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId_DoctorId",
                table: "AvailabilitySlot",
                columns: new[] { "AvailabilityRuleId", "DoctorId" },
                principalTable: "AvailabilityRule",
                principalColumns: new[] { "Id", "DoctorId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specialities_SpecialityId",
                table: "Doctors",
                column: "SpecialityId",
                principalTable: "Specialities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
