using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Controllers;
using System.Security.Claims;

namespace PropertyManagement.Tests;

[TestClass]
public class RentalApplicationsControllerTests
{
    [TestMethod]
    public async Task Withdraw_OwnSubmittedApplication_Succeeds()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;
        var application = CreateApplication("applicant-1", RentalApplicationStatus.Submitted);

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, "applicant-1");
        var result = await controller.Withdraw(application.Id);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(RentalApplicationStatus.Withdrawn, application.Status);
        Assert.IsNotNull(application.UpdatedAtUtc);
    }

    [TestMethod]
    public async Task Withdraw_ApprovedApplication_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;

        var application = CreateApplication("applicant-1",RentalApplicationStatus.Approved);

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, "applicant-1");
        var result = await controller.Withdraw(application.Id);

        Assert.IsInstanceOfType<ConflictObjectResult>(result);
        Assert.AreEqual(RentalApplicationStatus.Approved, application.Status);
    }

    [TestMethod]
    public async Task Withdraw_AnotherApplicantsApplication_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var dbContext = database.Context;

        var application = CreateApplication("applicant-2", RentalApplicationStatus.Submitted);

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, "applicant-1");
        var result = await controller.Withdraw(application.Id);

        Assert.IsInstanceOfType<NotFoundResult>(result);
        Assert.AreEqual(RentalApplicationStatus.Submitted, application.Status);
    }

    private static RentalApplication CreateApplication(string applicantUserId, RentalApplicationStatus status)
    {
        return new RentalApplication
        {
            Unit = new Unit
            {
                Property = new Property
                {
                    Name = "Test Property",
                    Address = "123 Test Street"
                },
                UnitType = new UnitType { Name = "Apartment" },
                UnitNumber = "101",
                Bedrooms = 2,
                MonthlyRent = 1500m
            },
            ApplicantUserId = applicantUserId,
            Status = status,
            FirstName = "Test",
            LastName = "Applicant",
            Email = "test@example.com"
        };
    }

    private static RentalApplicationsController CreateController(
        ApplicationDbContext dbContext,
        string applicantUserId)
    {
        var controller = new RentalApplicationsController(dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    applicantUserId)
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
