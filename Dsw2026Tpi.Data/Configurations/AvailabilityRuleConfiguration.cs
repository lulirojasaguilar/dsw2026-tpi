using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
    {
        public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
        {
            builder.ToTable("AvailabilityRules", table =>
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

            builder.HasKey(rule => rule.Id);

            builder.Property(rule => rule.Month)
                .IsRequired()
                .HasColumnType("tinyint");

            builder.Property(rule => rule.Year)
                .IsRequired()
                .HasColumnType("smallint");

            builder.Property(rule => rule.DayOfWeek)
                .IsRequired()
                .HasColumnType("smallint");

            builder.Property(rule => rule.StartTime)
                .IsRequired()
                .HasColumnType("time(0)");

            builder.Property(rule => rule.EndTime)
                .IsRequired()
                .HasColumnType("time(0)");

            builder.Property(rule => rule.Deleted)
                 .HasDefaultValue(false)
                 .HasColumnName("deleted");

            builder.Property(rule => rule.CreatedAt)
                 .IsRequired();

            builder.Property(rule => rule.UpdatedAt)
                  .IsRequired();

            builder.HasOne(rule => rule.Doctor)
                  .WithMany()
                  .HasForeignKey(rule => rule.DoctorId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

            /* Evita repetir exactamente la misma regla activa.
               La detección de solapamientos parciales debe realizarse
               igualmente desde AvailabilityService. */

            builder.HasIndex(rule => new
            {
                rule.DoctorId,
                rule.Year,
                rule.Month,
                rule.DayOfWeek,
                rule.StartTime,
                rule.EndTime
            })
                .IsUnique()
                .HasFilter("[deleted] = 0")
                .HasDatabaseName("UX_AvailabilityRules_ActiveRule");

            /* Clave alternativa necesaria para que AvailabilitySlot pueda referenciar conjuntamente: AvailabilityRule.Id + AvailabilityRule.DoctorId
               De esta manera se garantiza que el DoctorId del slot sea el mismo DoctorId de la regla.*/

            builder.HasAlternateKey(rule => new
            {
                rule.Id,
                rule.DoctorId
            })
                    .HasName("AK_AvailabilityRules_Id_DoctorId");
        }
    }
}
