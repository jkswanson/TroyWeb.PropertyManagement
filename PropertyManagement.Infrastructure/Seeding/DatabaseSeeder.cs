using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseSeeder(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();

        await SeedUnitTypesAsync();
        await SeedPropertiesAsync();
        await SeedUnitsAsync();

        await SeedSampleApplicantsAsync();
        await SeedRentalApplicationsAsync();
        await SeedSampleApplicationHistoriesAsync();
        await SeedSampleLeasesAsync();
    }

    private async Task SeedUnitTypesAsync()
    {
        if (await _dbContext.UnitTypes.AnyAsync())
            return;

        var unitTypes = new List<UnitType>
        {
            new() { Name = "Studio" },
            new() { Name = "Apartment" },
            new() { Name = "Townhouse" },
            new() { Name = "Condominium" }
        };

        await _dbContext.UnitTypes.AddRangeAsync(unitTypes);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedPropertiesAsync()
    {
        if (await _dbContext.Properties.AnyAsync())
            return;

        var faker = new Faker("en_US");
        var properties = new List<Property>();

        for (int i = 1; i <= 5; i++)
        {
            properties.Add(new Property
            {
                Name = $"Property {i}",
                Address = faker.Address.FullAddress()
            });
        }

        await _dbContext.Properties.AddRangeAsync(properties);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedUnitsAsync()
    {
        if (await _dbContext.Units.AnyAsync())
            return;

        var properties = await _dbContext.Properties.ToListAsync();
        var unitTypes = await _dbContext.UnitTypes.Where(t => t.IsActive).ToListAsync();
        var faker = new Faker();
        var units = new List<Unit>();

        foreach (var property in properties)
        {
            for (int i = 1; i <= 10; i++)
            {
                var unitType = faker.PickRandom(unitTypes);
                units.Add(new Unit
                {
                    PropertyId = property.Id,
                    UnitNumber = i.ToString("D3"),
                    Bedrooms = faker.Random.Int(0, 4),
                    MonthlyRent = faker.Random.Int(900, 3500),
                    UnitTypeId = unitType.Id
                });
            }
        }

        await _dbContext.Units.AddRangeAsync(units);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles =
        [
            RoleNames.Applicant,
            RoleNames.PropertyManager
        ];

        foreach (var role in roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
                continue;

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{role}': " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        await SeedUserAsync("applicant@example.com", "Test Applicant", RoleNames.Applicant);
        await SeedUserAsync("applicant2@example.com", "Test Applicant 2", RoleNames.Applicant);
        await SeedUserAsync("manager@example.com", "Test Property Manager", RoleNames.PropertyManager);
    }

    private async Task SeedUserAsync(string email, string fullName, string role)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "Test1234!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user '{email}': " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{role}' to '{email}': " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedSampleApplicantsAsync()
    {
        var faker = new Faker("en_US")
        {
            Random = new Randomizer(2026)
        };

        for (int i = 1; i <= 10; i++)
        {
            var email = $"demo.applicant{i:D2}@example.com";
            if (await _userManager.FindByEmailAsync(email) != null)
                continue;

            var fullName = faker.Name.FullName();
            await SeedUserAsync(email, fullName, RoleNames.Applicant);
        }
    }


    private async Task SeedRentalApplicationsAsync()
    {
        var demoEmails = Enumerable.Range(1, 10)
            .Select(i => $"demo.applicant{i:D2}@example.com")
            .ToList();

        var applicants = new List<ApplicationUser>();
        foreach (var email in demoEmails)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
                applicants.Add(user);
        }

        if (applicants.Count == 0)
            return;

        var applicantIds = applicants.Select(a => a.Id).ToList();

        // Do not generate another batch on subsequent application starts.
        if (await _dbContext.RentalApplications
            .AnyAsync(a => applicantIds.Contains(a.ApplicantUserId)))
        {
            return;
        }

        var units = await _dbContext.Units
            .OrderBy(u => u.Id)
            .ToListAsync();

        if (units.Count == 0)
            return;

        var faker = new Faker("en_US")
        {
            Random = new Randomizer(2026)
        };

        var statuses = new[]
        {
            RentalApplicationStatus.Draft,
            RentalApplicationStatus.Submitted,
            RentalApplicationStatus.Returned,
            RentalApplicationStatus.Approved,
            RentalApplicationStatus.Denied,
            RentalApplicationStatus.Withdrawn
        };

        var applications = new List<RentalApplication>();

        for (int i = 0; i < 25; i++)
        {
            var applicant = applicants[i % applicants.Count];
            var name = applicant.FullName.Split(' ', 2);
            var status = statuses[i % statuses.Length];
            var createdAt = DateTime.UtcNow.Date.AddDays(-(i + 5));
            var application = new RentalApplication
            {
                UnitId = units[i % units.Count].Id,
                ApplicantUserId = applicant.Id,
                Status = status,
                FirstName = name[0],
                LastName = name.Length > 1 ? name[1] : "Applicant",
                Email = applicant.Email ?? string.Empty,
                PhoneNumber = faker.Phone.PhoneNumber("###-###-####"),
                CurrentAddress = faker.Address.FullAddress(),
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = status == RentalApplicationStatus.Draft ? null : createdAt.AddDays(2),
                SubmittedAtUtc = status == RentalApplicationStatus.Draft ? null: createdAt.AddDays(1)
            };

            var residenceCount = faker.Random.Int(1, 3);
            for (int j = 0; j < residenceCount; j++)
            {
                var moveIn = DateTime.UtcNow.Date
                    .AddYears(-(j + 2))
                    .AddMonths(-i);

                application.ResidenceHistories.Add(new ResidenceHistory
                {
                    Address = faker.Address.FullAddress(),
                    MoveInDate = moveIn,
                    MoveOutDate = j == 0? null : moveIn.AddMonths(10),
                    LandlordName = faker.Name.FullName(),
                    LandlordPhone = faker.Phone.PhoneNumber("###-###-####")
                });
            }

            applications.Add(application);
        }

        await _dbContext.RentalApplications.AddRangeAsync(applications);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSampleApplicationHistoriesAsync()
    {
        var demoEmails = Enumerable.Range(1, 10)
            .Select(i => $"demo.applicant{i:D2}@example.com")
            .ToList();

        var manager = await _userManager.FindByEmailAsync("manager@example.com");

        if (manager == null)
            return;

        var applications = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a => demoEmails.Contains(a.Email!))
            .Select(a => new
            {
                a.Id,
                a.Status,
                a.CreatedAtUtc,
                a.SubmittedAtUtc
            })
            .ToListAsync();

        var applicationIds = applications.Select(a => a.Id).ToList();

        var existingHistoryIds = await _dbContext.RentalApplicationStatusHistories
            .Where(h => applicationIds.Contains(h.RentalApplicationId))
            .Select(h => h.RentalApplicationId)
            .Distinct()
            .ToListAsync();

        var existingIds = existingHistoryIds.ToHashSet();
        var histories = new List<RentalApplicationStatusHistory>();

        foreach (var application in applications)
        {
            // Never overwrite or supplement existing review history.
            if (existingIds.Contains(application.Id))
                continue;

            if (application.Status == RentalApplicationStatus.Draft)
                continue;

            var submittedAt = application.SubmittedAtUtc
                ?? application.CreatedAtUtc.AddDays(1);

            if (application.Status == RentalApplicationStatus.Withdrawn)
            {
                histories.Add(new RentalApplicationStatusHistory
                {
                    RentalApplicationId = application.Id,
                    PreviousStatus = RentalApplicationStatus.Draft,
                    NewStatus = RentalApplicationStatus.Withdrawn,
                    CreatedAtUtc = submittedAt
                });

                continue;
            }

            histories.Add(new RentalApplicationStatusHistory
            {
                RentalApplicationId = application.Id,
                PreviousStatus = RentalApplicationStatus.Draft,
                NewStatus = RentalApplicationStatus.Submitted,
                CreatedAtUtc = submittedAt
            });

            if (application.Status == RentalApplicationStatus.Submitted)
                continue;

            histories.Add(new RentalApplicationStatusHistory
            {
                RentalApplicationId = application.Id,
                PreviousStatus = RentalApplicationStatus.Submitted,
                NewStatus = application.Status,
                ReviewerUserId = manager.Id,
                Comments = application.Status switch
                {
                    RentalApplicationStatus.Returned =>
                        "Please verify the applicant information and resubmit.",
                    RentalApplicationStatus.Denied =>
                        "Application did not meet the rental approval criteria.",
                    _ => null
                },
                CreatedAtUtc = submittedAt.AddDays(1)
            });
        }

        if (histories.Count == 0)
            return;

        await _dbContext.RentalApplicationStatusHistories.AddRangeAsync(histories);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSampleLeasesAsync()
    {
        var approvedApplications = await _dbContext.RentalApplications
            .Where(a => a.Status == RentalApplicationStatus.Approved)
            .OrderBy(a => a.Id)
            .ToListAsync();

        var existingLeases = await _dbContext.Leases.ToListAsync();

        foreach (var application in approvedApplications)
        {
            // Do not create duplicate leases.
            if (existingLeases.Any(l => l.RentalApplicationId == application.Id))
                continue;

            // Use different historical start dates for sample leases.
            var leaseStart = DateTime.SpecifyKind(
                DateTime.UtcNow.Date.AddMonths(-6),
                DateTimeKind.Utc);

            var leaseEnd = leaseStart.AddMonths(12);

            // Same overlap rule as the manager approval workflow.
            var overlaps = existingLeases.Any(l =>
                l.UnitId == application.UnitId &&
                l.StartDateUtc < leaseEnd &&
                l.EndDateUtc > leaseStart);

            if (overlaps)
                continue;

            var lease = new Lease
            {
                RentalApplicationId = application.Id,
                UnitId = application.UnitId,
                ApplicantUserId = application.ApplicantUserId,
                StartDateUtc = leaseStart,
                EndDateUtc = leaseEnd,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Leases.Add(lease);
            existingLeases.Add(lease);
        }

        await _dbContext.SaveChangesAsync();
    }
}
