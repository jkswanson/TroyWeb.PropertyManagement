using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.UnitTypes;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class UnitTypesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public UnitTypesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var unitTypes = await _dbContext.UnitTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new UnitTypeViewModel
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive,
                UnitCount = t.Units.Count
            })
            .ToListAsync();

        return View(unitTypes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UnitTypeFormViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _dbContext.UnitTypes
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new UnitTypeFormViewModel
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive
            })
            .FirstOrDefaultAsync();

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitTypeFormViewModel model)
    {
        var name = model.Name?.Trim() ?? string.Empty;

        //Validate UnitType name exists & isn't duplicated
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Unit type name is required.");
        }
        else if (await _dbContext.UnitTypes.AnyAsync(t => t.Name == name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A unit type with this name already exists.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var unitType = new UnitType
        {
            Name = name,
            IsActive = model.IsActive
        };

        _dbContext.UnitTypes.Add(unitType);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitTypeFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var unitType = await _dbContext.UnitTypes.FindAsync(id);

        if (unitType == null)
            return NotFound();

        var name = model.Name?.Trim() ?? string.Empty;

        //Validate UnitType name exists & isn't duplicated
        //excluding the current record
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Unit type name is required.");
        }
        else if (await _dbContext.UnitTypes.AnyAsync(t =>
            t.Id != id && t.Name == name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A unit type with this name already exists.");
        }

        if (!ModelState.IsValid)
            return View(model);

        unitType.Name = name;
        unitType.IsActive = model.IsActive;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}