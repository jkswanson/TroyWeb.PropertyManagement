using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.RentalApplications;
using System.Security.Claims;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = RoleNames.Applicant)]
public class ResidenceHistoriesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ResidenceHistoriesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int rentalApplicationId)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var canEdit = await _dbContext.RentalApplications
            .AnyAsync(a =>
                a.Id == rentalApplicationId &&
                a.ApplicantUserId == applicantUserId &&
                (a.Status == RentalApplicationStatus.Draft ||
                 a.Status == RentalApplicationStatus.Returned));

        if (!canEdit)
            return NotFound();

        return PartialView("_ResidenceHistoryForm", new ResidenceHistoryViewModel { RentalApplicationId = rentalApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = await _dbContext.ResidenceHistories
            .AsNoTracking()
            .Where(r =>
                r.Id == id &&
                r.RentalApplication.ApplicantUserId == applicantUserId &&
                (r.RentalApplication.Status == RentalApplicationStatus.Draft ||
                r.RentalApplication.Status == RentalApplicationStatus.Returned))
            .Select(r => new ResidenceHistoryViewModel
            {
                Id = r.Id,
                RentalApplicationId = r.RentalApplicationId,
                Address = r.Address,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone
            })
            .FirstOrDefaultAsync();

        if (model == null)
            return NotFound();

        return PartialView("_ResidenceHistoryForm", model);
    }


    [HttpGet]
    public async Task<IActionResult> List(int rentalApplicationId)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a =>
                a.Id == rentalApplicationId &&
                a.ApplicantUserId == applicantUserId)
            .Select(a => new { a.Status })
            .FirstOrDefaultAsync();

        if (application == null)
            return NotFound();

        if (application.Status != RentalApplicationStatus.Draft &&
            application.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        var residences = await _dbContext.ResidenceHistories
            .AsNoTracking()
            .Where(r => r.RentalApplicationId == rentalApplicationId)
            .OrderByDescending(r => r.MoveInDate)
            .Select(r => new ResidenceHistoryViewModel
            {
                Id = r.Id,
                RentalApplicationId = r.RentalApplicationId,
                Address = r.Address,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone
            })
            .ToListAsync();

        return PartialView("_ResidenceHistoryList", residences);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = await _dbContext.ResidenceHistories
            .AsNoTracking()
            .Where(r =>
                r.Id == id &&
                r.RentalApplication.ApplicantUserId == applicantUserId &&
                (r.RentalApplication.Status == RentalApplicationStatus.Draft ||
                 r.RentalApplication.Status == RentalApplicationStatus.Returned))
            .Select(r => new ResidenceHistoryViewModel
            {
                Id = r.Id,
                RentalApplicationId = r.RentalApplicationId,
                Address = r.Address,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone
            })
            .FirstOrDefaultAsync();

        if (model == null)
            return NotFound();

        return PartialView("_DeleteResidenceHistory", model);
    }

    [HttpGet]
    public async Task<IActionResult> ValidateSection(int rentalApplicationId)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a =>
                a.Id == rentalApplicationId &&
                a.ApplicantUserId == applicantUserId)
            .Select(a => new { a.Status })
            .FirstOrDefaultAsync();

        if (application == null)
            return NotFound();

        if (application.Status != RentalApplicationStatus.Draft &&
            application.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        bool hasResidenceHistory = await _dbContext.ResidenceHistories
            .AnyAsync(r => r.RentalApplicationId == rentalApplicationId);

        return Json(new
        {
            isValid = hasResidenceHistory,
            error = hasResidenceHistory ? (string?)null
                : "At least one residence history entry is required."
        });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ResidenceHistoryViewModel model)
    {
        var applicantUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var canEdit = await _dbContext.RentalApplications
            .AnyAsync(a =>
                a.Id == model.RentalApplicationId &&
                a.ApplicantUserId == applicantUserId &&
                (a.Status == RentalApplicationStatus.Draft ||
                 a.Status == RentalApplicationStatus.Returned));

        if (!canEdit)
            return NotFound();

        if (model.MoveInDate.HasValue &&
            model.MoveOutDate.HasValue &&
            model.MoveOutDate.Value < model.MoveInDate.Value)
        {
            ModelState.AddModelError(
                nameof(model.MoveOutDate),
                "Move Out Date cannot be earlier than Move In Date.");
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_ResidenceHistoryForm", model);
        }

        var residence = new ResidenceHistory
        {
            RentalApplicationId = model.RentalApplicationId,
            Address = model.Address.Trim(),
            MoveInDate = model.MoveInDate!.Value,
            MoveOutDate = model.MoveOutDate,
            LandlordName = model.LandlordName.Trim(),
            LandlordPhone = model.LandlordPhone.Trim()
        };

        _dbContext.ResidenceHistories.Add(residence);

        var application = await _dbContext.RentalApplications
            .FirstAsync(a => a.Id == model.RentalApplicationId);

        application.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResidenceHistoryViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var residence = await _dbContext.ResidenceHistories
            .Include(r => r.RentalApplication)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.RentalApplication.ApplicantUserId == applicantUserId);

        if (residence == null)
            return NotFound();

        if (residence.RentalApplication.Status != RentalApplicationStatus.Draft &&
            residence.RentalApplication.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        if (model.MoveInDate.HasValue && model.MoveOutDate.HasValue &&
            model.MoveOutDate.Value < model.MoveInDate.Value)
        {
            ModelState.AddModelError(nameof(model.MoveOutDate),
                "Move Out Date cannot be earlier than Move In Date.");
        }

        if (!ModelState.IsValid)
            return PartialView("_ResidenceHistoryForm", model);

        residence.Address = model.Address.Trim();
        residence.MoveInDate = model.MoveInDate!.Value;
        residence.MoveOutDate = model.MoveOutDate;
        residence.LandlordName = model.LandlordName.Trim();
        residence.LandlordPhone = model.LandlordPhone.Trim();
        residence.RentalApplication.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }


    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var residence = await _dbContext.ResidenceHistories
            .Include(r => r.RentalApplication)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.RentalApplication.ApplicantUserId == applicantUserId);

        if (residence == null)
            return NotFound();

        if (residence.RentalApplication.Status != RentalApplicationStatus.Draft &&
            residence.RentalApplication.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        residence.RentalApplication.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.ResidenceHistories.Remove(residence);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

}
