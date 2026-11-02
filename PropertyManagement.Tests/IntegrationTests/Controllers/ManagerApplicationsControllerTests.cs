using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels.ManagerApplications;
using System.Security.Claims;

namespace PropertyManagement.Tests.IntegrationTests.Controllers;

[TestClass]
public class ManagerApplicationsControllerTests
{
    [TestMethod]
    public async Task Review_ApprovalWithOverlappingLease_ReturnsValidationError()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;

        // Create a property and its unit.
        var property = new Property
        {
            Name = "Test Property",
            Address = "123 Test Street"
        };

        var unitType = new UnitType
        {
            Name = "Apartment"
        };

        var unit = new Unit
        {
            Property = property,
            UnitType = unitType,
            UnitNumber = "101",
            Bedrooms = 2,
            MonthlyRent = 1500m
        };

        // Existing approved application.
        var existingApplication = new RentalApplication
        {
            Unit = unit,
            ApplicantUserId = "applicant-1",
            Status = RentalApplicationStatus.Approved,
            FirstName = "Existing",
            LastName = "Tenant",
            Email = "existing@example.com"
        };

        // New submitted application for the same unit.
        var submittedApplication = new RentalApplication
        {
            Unit = unit,
            ApplicantUserId = "applicant-2",
            Status = RentalApplicationStatus.Submitted,
            FirstName = "New",
            LastName = "Applicant",
            Email = "new@example.com"
        };

        dbContext.RentalApplications.AddRange(existingApplication, submittedApplication);
        await dbContext.SaveChangesAsync();

        // Existing lease: December 1, 2026 - December 1, 2027.
        dbContext.Leases.Add(new Lease
        {
            RentalApplicationId = existingApplication.Id,
            UnitId = unit.Id,
            ApplicantUserId = existingApplication.ApplicantUserId,
            StartDateUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2027, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = submittedApplication.Id,
            Decision = RentalApplicationStatus.Approved,
            LeaseStartDate = new DateTime(2027, 1, 1)
        };

        // Act
        var result = await controller.Review(model);

        // Assert
        var redirect = result as RedirectToActionResult;
        var partial = result as PartialViewResult;

        Assert.IsNotNull(partial);
        Assert.AreEqual("_ReviewApplication", partial.ViewName);
        Assert.AreSame(model, partial.Model);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(
            controller.ModelState.TryGetValue(
                nameof(model.LeaseStartDate),
                out var dateErrors));
        Assert.Contains(e => e.ErrorMessage ==
            "This unit already has a lease overlapping the selected dates.", dateErrors.Errors);

        Assert.AreEqual(1, await dbContext.Leases.CountAsync());
        Assert.AreEqual(RentalApplicationStatus.Submitted, submittedApplication.Status);
        Assert.AreEqual(0, await dbContext.RentalApplicationStatusHistories
            .CountAsync(h => h.RentalApplicationId == submittedApplication.Id));
    }

    [TestMethod]
    public async Task Review_ValidApproval_CreatesTwelveMonthLease()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;
        var application = new RentalApplication
        {
            Unit = new Unit
            {
                Property = new Property
                {
                    Name = "Test Property",
                    Address = "123 Test Street"
                },
                UnitType = new UnitType { Name = "Apartment" },
                UnitNumber = "102",
                Bedrooms = 2,
                MonthlyRent = 1500m
            },
            ApplicantUserId = "applicant-1",
            Status = RentalApplicationStatus.Submitted,
            FirstName = "Test",
            LastName = "Applicant",
            Email = "test@example.com"
        };

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);
        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = application.Id,
            Decision = RentalApplicationStatus.Approved,
            LeaseStartDate = new DateTime(2026, 12, 1)
        };

        var result = await controller.Review(model);
        var json = result as JsonResult;

        Assert.IsNotNull(json);
        Assert.IsNotNull(json.Value);

        var successProperty = json.Value.GetType().GetProperty("success");
        Assert.IsNotNull(successProperty);
        Assert.AreEqual(true, successProperty.GetValue(json.Value));

        var lease = await dbContext.Leases.SingleAsync();
        Assert.AreEqual(
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), lease.StartDateUtc);

        Assert.AreEqual(
            new DateTime(2027, 12, 1, 0, 0, 0, DateTimeKind.Utc), lease.EndDateUtc);

        Assert.AreEqual(RentalApplicationStatus.Approved, application.Status);

        Assert.AreEqual(1, await dbContext.RentalApplicationStatusHistories.CountAsync());
    }

    [TestMethod]
    public async Task Review_ApprovalWithoutStartDate_DoesNotCreateLease()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;
        var controller = CreateController(dbContext);
        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = 1,
            Decision = RentalApplicationStatus.Approved,
            LeaseStartDate = null
        };

        var result = await controller.Review(model);
        var partial = result as PartialViewResult;

        Assert.IsNotNull(partial);
        Assert.AreEqual("_ReviewApplication", partial.ViewName);
        Assert.AreSame(model, partial.Model);

        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(controller.ModelState.TryGetValue(nameof(model.LeaseStartDate), out var dateErrors));
        Assert.Contains(
            e => e.ErrorMessage == "A lease start date is required when approving an application.",
            dateErrors.Errors);

        Assert.AreEqual(0, await dbContext.Leases.CountAsync());
    }

    [TestMethod]
    public async Task Review_ApprovalWithTodayStartDate_DoesNotCreateLease()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;
        var controller = CreateController(dbContext);
        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = 1,
            Decision = RentalApplicationStatus.Approved,
            LeaseStartDate = DateTime.Today
        };

        // Act
        var result = await controller.Review(model);

        // Assert
        var partial = result as PartialViewResult;
        Assert.IsNotNull(partial);
        Assert.AreEqual("_ReviewApplication", partial.ViewName);
        Assert.AreSame(model, partial.Model);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(controller.ModelState
            .TryGetValue(nameof(model.LeaseStartDate), out var dateErrors));
        Assert.Contains(e =>
            e.ErrorMessage == "The lease start date must be tomorrow or later.", dateErrors.Errors);
        Assert.AreEqual(0, await dbContext.Leases.CountAsync());
        Assert.AreEqual(0, await dbContext.RentalApplicationStatusHistories.CountAsync());
    }

    [TestMethod]
    public async Task Review_ReturnWithoutComments_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = CreateController(database.Context);
        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = 1,
            Decision = RentalApplicationStatus.Returned,
            Comments = ""
        };

        var result = await controller.Review(model);
        var partial = result as PartialViewResult;

        Assert.IsNotNull(partial);
        Assert.AreEqual("_ReviewApplication", partial.ViewName);
        Assert.AreSame(model, partial.Model);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(
            controller.ModelState.ContainsKey(nameof(model.Comments)));
        Assert.AreEqual(
            0,
            await database.Context.RentalApplicationStatusHistories.CountAsync());
    }

    [TestMethod]
    public async Task Review_DenyWithoutComments_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = CreateController(database.Context);
        var model = new ManagerApplicationReviewViewModel
        {
            ApplicationId = 1,
            Decision = RentalApplicationStatus.Denied,
            Comments = "   "
        };

        var result = await controller.Review(model);
        var partial = result as PartialViewResult;

        Assert.IsNotNull(partial);
        Assert.AreEqual("_ReviewApplication", partial.ViewName);
        Assert.AreSame(model, partial.Model);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(model.Comments)));
        Assert.AreEqual(0, await database.Context.RentalApplicationStatusHistories.CountAsync());
    }

    private static ManagerApplicationsController CreateController(ApplicationDbContext dbContext)
    {
        var controller = new ManagerApplicationsController(dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [
                                new Claim(ClaimTypes.NameIdentifier, "manager-1")
                            ],
                            "TestAuthentication"))
                }
            }
        };

        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            new TestTempDataProvider());

        return controller;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
            => new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
