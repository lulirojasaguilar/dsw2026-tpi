using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("Specialties");

        builder.HasKey(speciality => speciality.Id);

        builder.Property(speciality => speciality.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(speciality => speciality.Description)
             .IsRequired()
             .HasMaxLength(100);

        builder.Property(speciality => speciality.Deleted)
            .HasDefaultValue(false)
            .HasColumnName("deleted");

        builder.Property(speciality => speciality.CreatedAt)
            .IsRequired();

        builder.Property(speciality => speciality.UpdatedAt)
            .IsRequired();

        builder.HasIndex(speciality => speciality.Name)
            .IsUnique()
            .HasFilter("[deleted] = 0")
            .HasDatabaseName("UX_Specialties_Name_Active");

    }
}
