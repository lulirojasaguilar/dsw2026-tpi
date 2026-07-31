using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Dni)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("varchar(10)");

            builder.HasIndex(p => p.Dni)
                .IsUnique()
                .HasDatabaseName("UX_Patients_Dni");

            builder.Property(p => p.FullName)
                .HasMaxLength(150);

            builder.Property(p => p.Deleted)
                .HasDefaultValue(false)
                .HasColumnName("deleted");

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.HasIndex(p => p.ApplicationUserId)
                .IsUnique()
                .HasDatabaseName("UX_Patients_ApplicationUserId");

        }
    }
}
