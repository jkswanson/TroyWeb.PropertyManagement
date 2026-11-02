using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Constants;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels.ManagerApplications;
using PropertyManagement.Web.ViewModels.RentalApplications;
using System.Data;
using System.Security.Claims;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class ManagerApplicationsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ManagerApplicationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(RentalApplicationStatus? status, int? propertyId)
    {
        var query = _dbContext.RentalApplications.AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (propertyId.HasValue)
        {
            query = query.Where(a => a.Unit.PropertyId == propertyId.Value);
        }

        var applications = await query
            .OrderByDescending(a => a.SubmittedAtUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Select(a => new ManagerApplicationListViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                ApplicantName = a.FirstName + " " + a.LastName,
                Status = a.Status,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc
            })
            .ToListAsync();

        var properties = await _dbContext.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToListAsync();

        var model = new ManagerApplicationIndexViewModel
        {
            Status = status,
            PropertyId = propertyId,
            Properties = properties,
            Applications = applications
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new ManagerApplicationDetailsViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                Status = a.Status,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc,
                Application = new RentalApplicationWizardViewModel
                {
                    Id = a.Id,
                    UnitId = a.UnitId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    CurrentAddress = a.CurrentAddress,
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
                },
                StatusHistory = a.StatusHistory
                    .OrderBy(h => h.CreatedAtUtc)
                    .ThenBy(h => h.Id)
                    .Select(h => new ManagerApplicationStatusHistoryViewModel
                    {
                        PreviousStatus = h.PreviousStatus,
                        NewStatus = h.NewStatus,
                        ReviewerUserId = h.ReviewerUserId,
                        Comments = h.Comments,
                        CreatedAtUtc = h.CreatedAtUtc
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (application == null)
            return NotFound();

        return View(application);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Status
            })
            .SingleOrDefaultAsync();

        if (application == null)
            return NotFound();

        if (application.Status != RentalApplicationStatus.Submitted)
            return Conflict("Only submitted applications can be reviewed.");

        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = application.Id
        };

        return PartialView("_ReviewApplication", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ManagerApplicationReviewViewModel model)
    {
        if (model.ApplicationId <= 0)
            return BadRequest();

        var reviewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(reviewerUserId))
            return Challenge();

        if (model.Decision != RentalApplicationStatus.Approved &&
            model.Decision != RentalApplicationStatus.Returned &&
            model.Decision != RentalApplicationStatus.Denied)
        {
            return BadRequest("Invalid review decision.");
        }

        if ((model.Decision == RentalApplicationStatus.Returned ||
             model.Decision == RentalApplicationStatus.Denied) &&
            string.IsNullOrWhiteSpace(model.Comments))
        {
            ModelState.AddModelError(
                nameof(model.Comments),
                "Comments are required when returning or denying an application.");
        }

        //Lease start date required
        if (model.Decision == RentalApplicationStatus.Approved &&
            !model.LeaseStartDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.LeaseStartDate),
                "A lease start date is required when approving an application.");
        }

        //Lease start date must be after today
        //Requirements say "same-day" leases not available
        if (model.Decision == RentalApplicationStatus.Approved &&
            model.LeaseStartDate.HasValue &&
            model.LeaseStartDate.Value.Date <= DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.LeaseStartDate),
                "The lease start date must be tomorrow or later.");
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_ReviewApplication", model);
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var application = await _dbContext.RentalApplications
            .SingleOrDefaultAsync(a => a.Id == model.ApplicationId);

        if (application == null)
            return NotFound();

        if (application.Status != RentalApplicationStatus.Submitted)
        {
            ModelState.AddModelError(string.Empty, "Only submitted applications can be reviewed.");
            return PartialView("_ReviewApplication", model);
        }

        var now = DateTime.UtcNow;
        var previousStatus = application.Status;

        if (model.Decision == RentalApplicationStatus.Approved)
        {
            var leaseStart = DateTime.SpecifyKind(
                model.LeaseStartDate!.Value.Date,
                DateTimeKind.Utc);

            var leaseEnd = leaseStart.AddMonths(12);

            var hasOverlappingLease = await _dbContext.Leases
                .AnyAsync(l =>
                    l.UnitId == application.UnitId &&
                    l.StartDateUtc < leaseEnd &&
                    l.EndDateUtc > leaseStart);

            if (hasOverlappingLease)
            {
                ModelState.AddModelError(nameof(model.LeaseStartDate),
                    "This unit already has a lease overlapping the selected dates.");

                return PartialView("_ReviewApplication", model);
            }

            var lease = new Lease
            {
                RentalApplicationId = application.Id,
                UnitId = application.UnitId,
                ApplicantUserId = application.ApplicantUserId,
                StartDateUtc = leaseStart,
                EndDateUtc = leaseEnd,
                CreatedAtUtc = now
            };

            _dbContext.Leases.Add(lease);
        }

        application.Status = model.Decision;
        application.UpdatedAtUtc = now;

        _dbContext.RentalApplicationStatusHistories.Add(
            new RentalApplicationStatusHistory
            {
                RentalApplicationId = application.Id,
                PreviousStatus = previousStatus,
                NewStatus = model.Decision,
                ReviewerUserId = reviewerUserId,
                Comments = model.Comments?.Trim(),
                CreatedAtUtc = now
            });

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = model.Decision switch
        {
            RentalApplicationStatus.Approved => "Application approved. A 12-month lease was created.",
            RentalApplicationStatus.Returned => "Application returned to applicant.",
            RentalApplicationStatus.Denied => "Application denied.",
            _ => "Application reviewed."
        };

        return Json(new { success = true });
    }
}
