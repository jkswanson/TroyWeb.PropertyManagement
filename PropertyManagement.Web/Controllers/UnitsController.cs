using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Services;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.Units;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class UnitsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public UnitsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var units = await _dbContext.Units
            .AsNoTracking()
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new UnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .ToListAsync();

        return View(units);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UnitFormViewModel();
        await PopulateDropdownsAsync(model);

        return PartialView("_UnitForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UnitFormViewModel
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeId = u.UnitTypeId
            })
            .FirstOrDefaultAsync();

        if (model == null)
            return NotFound();

        await PopulateDropdownsAsync(model);
        return PartialView("_UnitForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
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

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .FirstOrDefaultAsync();

        if (unit == null)
            return NotFound();

        return PartialView("_DeleteUnit", unit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        //Property exists?
        if (model.PropertyId.HasValue && !await _dbContext.Properties
                .AnyAsync(p => p.Id == model.PropertyId.Value))
        {
            ModelState.AddModelError(nameof(model.PropertyId),
                "The selected property does not exist.");
        }

        //UnitType exists & is active
        if (model.UnitTypeId.HasValue &&
            !await _dbContext.UnitTypes.AnyAsync(t =>
                t.Id == model.UnitTypeId.Value && t.IsActive))
        {
            ModelState.AddModelError(
                nameof(model.UnitTypeId),
                "Please select an active unit type.");
        }

        //If numeric only value compare on equal width
        //e.g. ("5" should be the same as "05" or "005")
        //for duplicate validations, creation, and edits
        var normalizedUnitNumber = 
            UnitNumberNormalizer.Normalize(model.UnitNumber ?? string.Empty);

        //Duplicate Unit Number Check
        if (model.PropertyId.HasValue &&
            !string.IsNullOrWhiteSpace(model.UnitNumber) &&
            await _dbContext.Units.AnyAsync(u =>
                u.PropertyId == model.PropertyId.Value &&
                u.UnitNumber == normalizedUnitNumber))
        {
            ModelState.AddModelError(nameof(model.UnitNumber),
                "This unit number already exists at the selected property.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return PartialView("_UnitForm", model);
        }

        var unit = new Unit
        {
            PropertyId = model.PropertyId!.Value,
            UnitNumber = normalizedUnitNumber,
            Bedrooms = model.Bedrooms,
            MonthlyRent = model.MonthlyRent,
            UnitTypeId = model.UnitTypeId!.Value
        };

        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var unit = await _dbContext.Units.FindAsync(id);

        if (unit == null)
            return NotFound();

        //If numeric only value compare on equal width
        //e.g. ("5" should be the same as "05" or "005")
        //for duplicate validations, creation, and edits
        var normalizedUnitNumber =
            UnitNumberNormalizer.Normalize(model.UnitNumber ?? string.Empty);

        //Property exists?
        if (model.PropertyId.HasValue &&
            !await _dbContext.Properties.AnyAsync(
                p => p.Id == model.PropertyId.Value))
        {
            ModelState.AddModelError(
                nameof(model.PropertyId),
                "The selected property does not exist.");
        }

        //UnitTypes must be active to be assigned
        //or already assigned an inactive UnitType to keep it
        if (model.UnitTypeId.HasValue)
        {
            var validType = await _dbContext.UnitTypes.AnyAsync(t =>
                t.Id == model.UnitTypeId.Value &&
                (t.IsActive || t.Id == unit.UnitTypeId));

            if (!validType)
            {
                ModelState.AddModelError(
                    nameof(model.UnitTypeId),
                    "Please select an active unit type.");
            }
        }

        // Duplicate Unit Number Check
        if (model.PropertyId.HasValue &&
            !string.IsNullOrWhiteSpace(normalizedUnitNumber) &&
            await _dbContext.Units.AnyAsync(u =>
                u.Id != id &&
                u.PropertyId == model.PropertyId.Value &&
                u.UnitNumber == normalizedUnitNumber))
        {
            ModelState.AddModelError(
                nameof(model.UnitNumber),
                "This unit number already exists at the selected property.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return PartialView("_UnitForm", model);
        }

        unit.PropertyId = model.PropertyId!.Value;
        unit.UnitNumber = normalizedUnitNumber;
        unit.Bedrooms = model.Bedrooms;
        unit.MonthlyRent = model.MonthlyRent;
        unit.UnitTypeId = model.UnitTypeId!.Value;

        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unit = await _dbContext.Units
            .Where(u => u.Id == id)
            .Select(u => new
            {
                Entity = u,
                PropertyName = u.Property.Name,
                UnitTypeName = u.UnitType.Name
            })
            .FirstOrDefaultAsync();

        if (unit == null)
            return NotFound();

        //Check related data
        bool hasApplications = await _dbContext.RentalApplications.AnyAsync(a => a.UnitId == id);
        bool hasLeases = await _dbContext.Leases.AnyAsync(l => l.UnitId == id);

        if (hasApplications || hasLeases)
        {
            ModelState.AddModelError(
                string.Empty,
                "This unit cannot be deleted because it has associated rental applications or leases.");

            var model = new UnitViewModel
            {
                Id = unit.Entity.Id,
                PropertyName = unit.PropertyName,
                UnitNumber = unit.Entity.UnitNumber,
                UnitTypeName = unit.UnitTypeName,
                Bedrooms = unit.Entity.Bedrooms,
                MonthlyRent = unit.Entity.MonthlyRent
            };

            return PartialView("_DeleteUnit", model);
        }

        _dbContext.Units.Remove(unit.Entity);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    private async Task PopulateDropdownsAsync(UnitFormViewModel model)
    {
        model.Properties = await _dbContext.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToListAsync();

        model.UnitTypes = await _dbContext.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == model.UnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.IsActive ? t.Name : $"{t.Name} (Inactive)"
            })
            .ToListAsync();
    }
}
