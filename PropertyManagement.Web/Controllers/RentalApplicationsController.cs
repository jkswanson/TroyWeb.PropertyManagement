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
public class RentalApplicationsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public RentalApplicationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        var applications = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a => a.ApplicantUserId == applicantUserId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Select(a => new RentalApplicationListViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                Status = a.Status,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc
            })
            .ToListAsync();

        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        var result = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a =>
                a.Id == id &&
                a.ApplicantUserId == applicantUserId)
            .Select(a => new
            {
                // Keep status outside the editable view model.
                a.Status,
                Model = new RentalApplicationWizardViewModel
                {
                    Id = a.Id,
                    UnitId = a.UnitId,
                    PropertyName = a.Unit.Property.Name,
                    UnitNumber = a.Unit.UnitNumber,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    CurrentAddress = a.CurrentAddress,
                    CanEdit = a.Status == RentalApplicationStatus.Draft ||
                        a.Status == RentalApplicationStatus.Returned,
                    Residences = a.ResidenceHistories
                        .OrderByDescending(r => r.MoveInDate)
                        .Select(r => new ResidenceHistoryViewModel
                        {
                            Id = r.Id,
                            Address = r.Address,
                            MoveInDate = r.MoveInDate,
                            MoveOutDate = r.MoveOutDate,
                            LandlordName = r.LandlordName,
                            LandlordPhone = r.LandlordPhone
                        })
                        .ToList()
                }
            })
            .FirstOrDefaultAsync();

        if (result == null)
            return NotFound();

        if (result.Status != RentalApplicationStatus.Draft &&
            result.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        return View(result.Model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a =>
                a.Id == id &&
                a.ApplicantUserId == applicantUserId)
            .Select(a => new
            {
                a.Status,
                a.CreatedAtUtc,
                a.SubmittedAtUtc,
                Model = new RentalApplicationWizardViewModel
                {
                    Id = a.Id,
                    UnitId = a.UnitId,
                    PropertyName = a.Unit.Property.Name,
                    UnitNumber = a.Unit.UnitNumber,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    CurrentAddress = a.CurrentAddress,
                    CanEdit = false,
                    Residences = a.ResidenceHistories
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
                        .ToList()
                }
            })
            .FirstOrDefaultAsync();

        if (application == null)
            return NotFound();

        ViewData["ApplicationStatus"] = application.Status;
        ViewData["CreatedAtUtc"] = application.CreatedAtUtc;
        ViewData["SubmittedAtUtc"] = application.SubmittedAtUtc;

        return View(application.Model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int unitId)
    {
        var unitExists = await _dbContext.Units.AnyAsync(u => u.Id == unitId);
        if (!unitExists)
            return NotFound();

        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        if (await HasActiveLeaseAsync(unitId))
        {
            TempData["ErrorMessage"] =
                "This unit is no longer available because it has an active lease.";

            return RedirectToAction("Index", "ApplicantUnits");
        }

        var application = new RentalApplication
        {
            UnitId = unitId,
            ApplicantUserId = applicantUserId,
            Status = RentalApplicationStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.RentalApplications.Add(application);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = application.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        RentalApplicationWizardViewModel model,
        int currentWizardStep = 1,
        string actionType = "SaveDraft")
    {
        if (id != model.Id)
            return BadRequest();

        if (actionType != "SaveDraft" && actionType != "Submit")
            return BadRequest();

        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        // Retrieve the application using the authenticated applicant's ID.
        // Display-only fields and ownership always come from the database.
        var application = await _dbContext.RentalApplications
            .Include(a => a.Unit)
                .ThenInclude(u => u.Property)
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.ApplicantUserId == applicantUserId);

        if (application == null)
            return NotFound();

        // Only Draft and Returned applications may be modified.
        if (application.Status != RentalApplicationStatus.Draft &&
            application.Status != RentalApplicationStatus.Returned)
        {
            return Forbid();
        }

        bool isSubmitting = actionType == "Submit";

        //if submitting app to a unit with an active lease - don't proceed
        if (isSubmitting && await HasActiveLeaseAsync(application.UnitId))  
            return Conflict("This unit is no longer available because it has an active lease.");
        

        // Additional validation applies only to final submission.
        if (isSubmitting)
        {
            bool hasResidenceHistory = await _dbContext.ResidenceHistories
                .AnyAsync(r => r.RentalApplicationId == application.Id);

            if (!hasResidenceHistory)
            {
                ModelState.AddModelError(string.Empty,
                    "At least one residence history entry is required.");
            }
        }

        // Validate posted applicant information and submission requirements.
        if (!ModelState.IsValid)
        {
            // Restore display-only values from the database.
            model.PropertyName = application.Unit.Property.Name;
            model.UnitNumber = application.Unit.UnitNumber;
            model.CanEdit = true;

            // Restore the residence list because it is not posted
            // as part of the main wizard form.
            model.Residences = await _dbContext.ResidenceHistories
                .AsNoTracking()
                .Where(r => r.RentalApplicationId == application.Id)
                .OrderByDescending(r => r.MoveInDate)
                .Select(r => new ResidenceHistoryViewModel
                {
                    Id = r.Id,
                    Address = r.Address,
                    MoveInDate = r.MoveInDate,
                    MoveOutDate = r.MoveOutDate,
                    LandlordName = r.LandlordName,
                    LandlordPhone = r.LandlordPhone
                })
                .ToListAsync();

            // Applicant Info errors should be shown on Step 1.
            // Otherwise, preserve the step the applicant was using.
            bool hasApplicantInfoErrors =
                ModelState[nameof(model.FirstName)]?.Errors.Count > 0 ||
                ModelState[nameof(model.LastName)]?.Errors.Count > 0 ||
                ModelState[nameof(model.Email)]?.Errors.Count > 0 ||
                ModelState[nameof(model.PhoneNumber)]?.Errors.Count > 0 ||
                ModelState[nameof(model.CurrentAddress)]?.Errors.Count > 0;

            ViewData["WizardStep"] = hasApplicantInfoErrors ? 1
                : Math.Clamp(currentWizardStep, 1, 3);

            return View(model);
        }

        // Explicitly assign only the applicant-editable fields.
        application.FirstName = model.FirstName.Trim();
        application.LastName = model.LastName.Trim();
        application.Email = model.Email.Trim();
        application.PhoneNumber = model.PhoneNumber.Trim();
        application.CurrentAddress = model.CurrentAddress.Trim();

        var now = DateTime.UtcNow;

        application.UpdatedAtUtc = now;

        if (isSubmitting)
        {
            application.Status = RentalApplicationStatus.Submitted;
            application.SubmittedAtUtc = now;
        }

        // Save applicant fields and status transition together.
        await _dbContext.SaveChangesAsync();

        if (isSubmitting)
        {
            TempData["SuccessMessage"] = "Application submitted successfully.";

            // Submitted applications cannot reopen the Edit wizard.
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Applicant information saved.";
        TempData["WizardStep"] = Math.Clamp(currentWizardStep, 1, 3);

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(applicantUserId))
            return Challenge();

        var application = await _dbContext.RentalApplications
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.ApplicantUserId == applicantUserId);

        if (application == null)
            return NotFound();

        // Only applications that have not reached a final decision can be withdrawn.
        if (application.Status != RentalApplicationStatus.Draft &&
            application.Status != RentalApplicationStatus.Returned &&
            application.Status != RentalApplicationStatus.Submitted)
        {
            return Conflict("This application cannot be withdrawn.");
        }

        application.Status = RentalApplicationStatus.Withdrawn;
        application.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Application withdrawn successfully.";

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> HasActiveLeaseAsync(int unitId)
    {
        var now = DateTime.UtcNow;
        return _dbContext.Leases.AnyAsync(l =>
            l.UnitId == unitId &&
            l.StartDateUtc <= now &&
            l.EndDateUtc > now);
    }
}
