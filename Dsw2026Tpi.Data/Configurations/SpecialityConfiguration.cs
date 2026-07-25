using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("Specialities");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(speciality => speciality.Description)
             .IsRequired()
             .HasMaxLength(100);

        builder.Property(speciality => speciality.Deleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(speciality => speciality.Name)
            .IsUnique()
            .HasFilter("[Deleted] = 0");
    }
}
