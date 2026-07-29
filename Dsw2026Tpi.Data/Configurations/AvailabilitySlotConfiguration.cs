using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {

            builder.ToTable("AvailabilitySlot", table =>
            {
                table.HasCheckConstraint(
                    "CK_AvailabilitySlot_TimeRange",
                    "[StartTime] < [EndTime]");

                table.HasCheckConstraint(
                    "CK_AvailabilitySlot_Status",
                    "[Status] IN ('AVAILABLE', 'BOOKED', 'BLOCKED')");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvailabilityRuleId).IsRequired();
            
            builder.Property(x => x.DoctorId).IsRequired();
            
            builder.Property(x => x.SlotDate)
                    .HasColumnType("date")
                    .IsRequired();
            
            builder.Property(x => x.StartTime)
                     .HasColumnType("time(0)")
                     .IsRequired();

            builder.Property(x => x.EndTime)
                     .HasColumnType("time(0)")
                     .IsRequired();

            builder.Property(x => x.Status)
                     .HasMaxLength(20)
                     .IsRequired();
            
            builder.Property(x => x.Deleted)
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.Property(x => x.RowVersion)
                    .IsRowVersion()
                    .IsConcurrencyToken();


            /* Relación compuesta con AvailabilityRule.
               No alcanza con comprobar que AvailabilityRuleId y DoctorId existan por separado. Ambos deben corresponder a la misma regla.
              
               Ejemplo que ahora quedará impedido:
              
               AvailabilityRuleId = regla del Doctor A
               DoctorId = Doctor B */

            builder.HasOne<AvailabilityRule>()
                .WithMany()
                .HasForeignKey(x => new
                {
                    x.AvailabilityRuleId,
                    x.DoctorId
                })
                .HasPrincipalKey(x => new
                {
                    x.Id,
                    x.DoctorId
                })
                .OnDelete(DeleteBehavior.Restrict);

            //Impide crear dos slots activos para el mismo médico, en la misma fecha y en la misma hora de inicio.

            builder.HasIndex(x => new
            {
                x.DoctorId,
                x.SlotDate,
                x.StartTime
            })
            .IsUnique()
            .HasFilter("[Deleted] = 0")
            .HasDatabaseName("UX_AvailabilitySlot_Doctor_Date_StartTime");
        }
    }
}
