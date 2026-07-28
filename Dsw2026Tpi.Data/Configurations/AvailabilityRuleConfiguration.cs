using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
    {
        public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
        {
            builder.ToTable("AvailabilityRule");
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

            
            builder.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasIndex(r => new { r.DoctorId, r.Year, r.Month, r.DayOfWeek, r.StartTime, r.EndTime })
                .IsUnique()
                .HasFilter("[Deleted] = 0")
                .HasDatabaseName("UX_AvailabilityRule_Doctor_Time");
        }
    }
}
