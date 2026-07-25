using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {
            builder.ToTable("AvailabilitySlot");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvailabilityRuleId).IsRequired();
            builder.Property(x => x.DoctorId).IsRequired();
            builder.Property(x => x.SlotDate)
                    .HasColumnName("SlotDate")
                    .IsRequired();
            builder.Property(x => x.StartTime).IsRequired();
            builder.Property(x => x.EndTime).IsRequired();
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Deleted)
                    .HasDefaultValue(false)
                    .IsRequired();

          
            builder.HasOne<AvailabilityRule>()
                .WithMany()
                .HasForeignKey(x => x.AvailabilityRuleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasIndex(s => new {s.DoctorId,  s.SlotDate, s.StartTime })
                .IsUnique()
                .HasFilter("[Deleted] = 0");
        }
    }
}
