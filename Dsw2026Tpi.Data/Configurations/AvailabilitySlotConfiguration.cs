using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {

            builder.ToTable("AvailabilitySlots", table =>
            {
                table.HasCheckConstraint(
                    "CK_AvailabilitySlot_TimeRange",
                    "[StartTime] < [EndTime]");

                table.HasCheckConstraint(
                    "CK_AvailabilitySlot_Status",
                    $"[Status] IN (" +
                    $"'{AvailabilityStatuses.Available}', " +
                    $"'{AvailabilityStatuses.Booked}', " +
                    $"'{AvailabilityStatuses.Blocked}')");
            });

            builder.HasKey(slot => slot.Id);

            builder.Property(slot => slot.SlotDate)
                .IsRequired()
                .HasColumnType("date");

            builder.Property(slot => slot.StartTime)
                 .IsRequired()
                 .HasColumnType("time(0)");

            builder.Property(slot => slot.EndTime)
                 .IsRequired()
                 .HasColumnType("time(0)");

            builder.Property(slot => slot.Status)
                 .IsRequired()
                 .HasMaxLength(20);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_AvailabilitySlots_Status",
                $"[Status] IN ('" +
                $"{AvailabilityStatuses.Available}', " +
                $"'{AvailabilityStatuses.Booked}', " +
                $"'{AvailabilityStatuses.Blocked}')"));

            builder.Property(slot => slot.Deleted)
                    .HasDefaultValue(false)
                    .HasColumnName("deleted");

            builder.Property(slot => slot.CreatedAt)
                    .IsRequired();

            builder.Property(slot => slot.UpdatedAt)
                    .IsRequired();


            /* Relación compuesta con AvailabilityRule.
               No alcanza con comprobar que AvailabilityRuleId y DoctorId existan por separado. Ambos deben corresponder a la misma regla.
              
               Ejemplo que ahora quedará impedido:
              
               AvailabilityRuleId = regla del Doctor A
               DoctorId = Doctor B */

            builder.HasOne<AvailabilityRule>()
                .WithMany()
                .HasForeignKey(slot => new
                {
                    slot.AvailabilityRuleId,
                    slot.DoctorId
                })
                .HasPrincipalKey(slot => new
                {
                    slot.Id,
                    slot.DoctorId
                })
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            //Impide crear dos slots activos para el mismo médico, en la misma fecha y en la misma hora de inicio.

            builder.HasIndex(slot => new
            {
                slot.DoctorId,
                slot.SlotDate,
                slot.StartTime
            })
            .IsUnique()
            .HasFilter("[deleted] = 0")
            .HasDatabaseName("UX_AvailabilitySlots_Doctor_Date_StartTime");
        }
    }
}
