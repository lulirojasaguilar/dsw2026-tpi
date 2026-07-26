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
            builder.ToTable("Appointment", table =>
            {
                table.HasCheckConstraint(
                    "CK_Appointment_Status",
                    "[Status] IN ('BOOKED', 'CANCELLED', 'ATTENDED', 'NO_SHOW')");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DoctorId)
                .IsRequired();

            builder.Property(x => x.AvailabilityId)
                .IsRequired();

            builder.Property(x => x.PatientId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CancelledAt)
                .IsRequired(false);

            builder.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AvailabilitySlot>()
                .WithMany()
                .HasForeignKey(x => x.AvailabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.AvailabilityId)
                .IsUnique()
                .HasDatabaseName("UX_Appointment_AvailabilityId");
        }
    }
}
