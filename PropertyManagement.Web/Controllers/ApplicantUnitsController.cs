using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.Units;

namespace PropertyManagement.Web.Controllers;

//Allow the manager to view as the applicant view without changing accounts
[Authorize(Roles = RoleNames.Applicant + "," + RoleNames.PropertyManager)]
public class ApplicantUnitsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicantUnitsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? propertyId,
        int? minimumBedrooms)
    {
        var query = _dbContext.Units.AsNoTracking();

        //Exclude units with active leases
        var now = DateTime.UtcNow;
        query = query.Where(u => !_dbContext.Leases.Any(l =>
            l.UnitId == u.Id &&
            l.StartDateUtc <= now &&
            l.EndDateUtc > now)
        );

        if (propertyId.HasValue)
        {
            query = query.Where(u => u.PropertyId == propertyId.Value);
        }

        if (minimumBedrooms.HasValue)
        {
            query = query.Where(u => u.Bedrooms >= minimumBedrooms.Value);
        }

        var model = new ApplicantUnitSearchViewModel
        {
            PropertyId = propertyId,
            MinimumBedrooms = minimumBedrooms,

            Properties = await _dbContext.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync(),

            Units = await query
                .OrderBy(u => u.Property.Name)
                .ThenBy(u => u.UnitNumber)
                .Select(u => new AvailableUnitViewModel
                {
                    Id = u.Id,
                    PropertyName = u.Property.Name,
                    PropertyAddress = u.Property.Address,
                    UnitNumber = u.UnitNumber,
                    UnitTypeName = u.UnitType.Name,
                    Bedrooms = u.Bedrooms,
                    MonthlyRent = u.MonthlyRent
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new AvailableUnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
                PropertyAddress = u.Property.Address,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .FirstOrDefaultAsync();

        if (unit == null)
            return NotFound();

        return View(unit);
    }
}

