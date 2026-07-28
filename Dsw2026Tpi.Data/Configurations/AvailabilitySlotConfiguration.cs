using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

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


            builder.Property(x => x.StartTime)
                 .HasColumnType("time(0)")
                 .IsRequired();


            builder.Property(x => x.EndTime)
                 .HasColumnType("time(0)")
                 .IsRequired();


            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .HasDefaultValue("AVAILABLE")
                .IsRequired();
            builder.Property(x => x.Deleted)
                .HasColumnName("deleted")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.SlotDate)
                .HasColumnType("date")
                .HasColumnName("Date")
                .IsRequired();


            builder.HasOne<AvailabilityRule>()
                .WithMany()
                .HasForeignKey(x => x.AvailabilityRuleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasIndex(x => new
            {
                x.DoctorId,
                x.SlotDate,
                x.StartTime
            })
            .IsUnique()
            .HasFilter("[deleted] = 0")
            .HasDatabaseName("UX_AvailabilitySlot_Doctor_Date_StartTime");
        }
    }
}
