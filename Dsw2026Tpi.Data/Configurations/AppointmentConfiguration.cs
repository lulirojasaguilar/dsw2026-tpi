using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AppointmentConfiguration
        : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Appointment_Status",
                    $"[Status] IN (" +
                    $"'{AppointmentStatuses.Booked}', " +
                    $"'{AppointmentStatuses.Cancelled}', " +
                    $"'{AppointmentStatuses.Attended}', " +
                    $"'{AppointmentStatuses.NoShow}')");
            });

            builder.HasKey(appointment => appointment.Id);

            builder.Property(appointment => appointment.Reason)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(appointment => appointment.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(appointment => appointment.CancelledAt);

            builder.Property(appointment => appointment.AttendedAt);

            builder.Property(appointment => appointment.CreatedAt)
                .IsRequired();

            builder.Property(appointment => appointment.UpdatedAt)
                .IsRequired();

            builder.HasOne(appointment => appointment.AvailabilitySlot)
                .WithMany()
                .HasForeignKey(appointment => appointment.AvailabilitySlotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(appointment => appointment.Patient)
                .WithMany()
                .HasForeignKey(appointment => appointment.PatientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(appointment => appointment.Doctor)
                .WithMany()
                .HasForeignKey(appointment => appointment.DoctorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(appointment => appointment.AvailabilitySlotId)
                .IsUnique()
                .HasFilter(
                    $"[Status] = '{AppointmentStatuses.Booked}'")
                .HasDatabaseName(
                    "UX_Appointments_AvailabilitySlotId_Booked");

        }
    }
}
