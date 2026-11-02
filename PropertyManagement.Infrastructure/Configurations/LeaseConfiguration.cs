using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;


namespace PropertyManagement.Infrastructure.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.RentalApplication)
            .WithMany()
            .HasForeignKey(l => l.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.RentalApplicationId)
            .IsUnique();

        builder.Property(l => l.ApplicantUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(l => l.StartDateUtc)
            .IsRequired();

        builder.Property(l => l.EndDateUtc)
            .IsRequired();

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();
    }
}
