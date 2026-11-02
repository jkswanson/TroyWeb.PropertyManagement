using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();

    public DbSet<ResidenceHistory> ResidenceHistories => Set<ResidenceHistory>();

    public DbSet<RentalApplicationStatusHistory> RentalApplicationStatusHistories => 
        Set<RentalApplicationStatusHistory>();

    public DbSet<Lease> Leases => Set<Lease>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
