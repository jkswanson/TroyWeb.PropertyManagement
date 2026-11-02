using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Infrastructure.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(u => u.Bedrooms)
            .IsRequired();

        builder.Property(u => u.MonthlyRent)
            .HasPrecision(18, 2);

        builder.HasIndex(u => new
        {
            u.PropertyId,
            u.UnitNumber
        }).IsUnique();
    }
}
