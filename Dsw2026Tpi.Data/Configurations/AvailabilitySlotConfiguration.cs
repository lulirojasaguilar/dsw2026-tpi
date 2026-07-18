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
            builder.Property(x => x.SlotDate).IsRequired();
            builder.Property(x => x.StartTime).IsRequired();
            builder.Property(x => x.EndTime).IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Deleted).IsRequired();

            builder.HasOne<AvailabilityRule>()
                .WithMany()
                .HasForeignKey(x =>  x.AvailabilityRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
