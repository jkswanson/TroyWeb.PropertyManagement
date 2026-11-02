using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Infrastructure.Configurations;

public class RentalApplicationStatusHistoryConfiguration
    : IEntityTypeConfiguration<RentalApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<RentalApplicationStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.RentalApplication)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(h => h.PreviousStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(h => h.NewStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(h => h.ReviewerUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(h => h.Comments)
            .HasMaxLength(2000);

        builder.Property(h => h.CreatedAtUtc)
            .IsRequired();
    }
}

