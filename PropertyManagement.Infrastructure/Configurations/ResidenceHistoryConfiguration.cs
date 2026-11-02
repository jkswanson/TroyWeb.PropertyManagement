using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Infrastructure.Configurations;

public class ResidenceHistoryConfiguration : IEntityTypeConfiguration<ResidenceHistory>
{
    public void Configure(EntityTypeBuilder<ResidenceHistory> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.LandlordName)
            .HasMaxLength(150);

        builder.Property(r => r.LandlordPhone)
            .HasMaxLength(20);

        builder.HasOne(r => r.RentalApplication)
            .WithMany(a => a.ResidenceHistories)
            .HasForeignKey(r => r.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.RentalApplicationId);
    }
}
