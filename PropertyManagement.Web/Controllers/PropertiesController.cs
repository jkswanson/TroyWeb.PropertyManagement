using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.Properties;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public PropertiesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Select(p => new PropertyViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                UnitCount = p.Units.Count
            })
            .ToListAsync();

        return View(properties);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_PropertyForm", new PropertyFormViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            })
            .FirstOrDefaultAsync();

        if (property == null)
            return NotFound();

        return PartialView("_PropertyForm", property);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                UnitCount = p.Units.Count
            })
            .FirstOrDefaultAsync();

        if (property == null)
            return NotFound();

        return View(property);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                UnitCount = p.Units.Count
            })
            .FirstOrDefaultAsync();

        if (property == null)
            return NotFound();

        return PartialView("_DeleteProperty", property);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
            return PartialView("_PropertyForm", model);

        var property = new Property
        {
            Name = model.Name.Trim(),
            Address = model.Address.Trim()
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return PartialView("_PropertyForm", model);

        var property = await _dbContext.Properties.FindAsync(id);

        if (property == null)
            return NotFound();

        property.Name = model.Name.Trim();
        property.Address = model.Address.Trim();

        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var property = await _dbContext.Properties.FindAsync(id);
        if (property == null)
            return NotFound();

        // Properties containing units cannot be deleted.
        if (await _dbContext.Units.AnyAsync(u => u.PropertyId == id))
        {
            ModelState.AddModelError(
                string.Empty,
                "This property cannot be deleted because it contains units.");

            var model = new PropertyViewModel
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                UnitCount = await _dbContext.Units.CountAsync(u => u.PropertyId == id)
            };

            return PartialView("_DeleteProperty", model);
        }

        _dbContext.Properties.Remove(property);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }
}