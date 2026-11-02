using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Infrastructure.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FirstName)
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .HasMaxLength(100);

        builder.Property(a => a.Email)
            .HasMaxLength(256);

        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(a => a.CurrentAddress)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(a => a.ApplicantUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(a => a.Unit)
            .WithMany()
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ApplicantUserId);

        builder.HasIndex(a => a.UnitId);
    }
}
