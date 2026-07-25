using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
    {
        public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
        {
            builder.ToTable("AvailabilityRule", table =>
            {
                table.HasCheckConstraint(
                    "CK_AvailabilityRule_Month",
                    "[Month] BETWEEN 1 AND 12");

                table.HasCheckConstraint(
                    "CK_AvailabilityRule_DayOfWeek",
                    "[DayOfWeek] BETWEEN 0 AND 6");

                table.HasCheckConstraint(
                    "CK_AvailabilityRule_TimeRange",
                    "[StartTime] < [EndTime]");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DoctorId).IsRequired();

            builder.Property(x => x.Month).IsRequired();

            builder.Property(x => x.Year).IsRequired();

            builder.Property(x => x.DayOfWeek).IsRequired();

            builder.Property(x => x.StartTime)
                .HasColumnType("time(0)")
                .IsRequired();

            builder.Property(x => x.EndTime)
                .HasColumnType("time(0)")
                .IsRequired();

            builder.Property(x => x.Deleted)
                    .HasDefaultValue(false)
                    .IsRequired();

            // Representa la relacion entre AvailabilityRule con Doctor.Impide crear reglas con un DoctorId inexistente.
            
            builder.HasOne<Doctor>()
                    .WithMany()
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

            /* Clave alternativa necesaria para que AvailabilitySlot pueda referenciar conjuntamente: AvailabilityRule.Id + AvailabilityRule.DoctorId
               De esta manera se garantiza que el DoctorId del slot sea el mismo DoctorId de la regla.*/

            builder.HasAlternateKey(x => new
                    {
                         x.Id,
                         x.DoctorId
                     })
                    .HasName("AK_AvailabilityRule_Id_DoctorId");

            /* Evita repetir exactamente la misma regla activa.
               La detección de solapamientos parciales debe realizarse
               igualmente desde AvailabilityService. */

            builder.HasIndex(x => new
            {
                x.DoctorId,
                x.Year,
                x.Month,
                x.DayOfWeek,
                x.StartTime,
                x.EndTime
            })
            .IsUnique()
            .HasFilter("[Deleted] = 0")
            .HasDatabaseName("UX_AvailabilityRule_ActiveRule");

        }
    }
}
