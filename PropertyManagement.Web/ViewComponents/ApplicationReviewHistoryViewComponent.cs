using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Web.ViewComponents;

/// <summary>
/// ViewComponent use requirement
/// </summary>
public class ApplicationReviewHistoryViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationReviewHistoryViewComponent(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var history = await _dbContext.RentalApplicationStatusHistories
            .AsNoTracking()
            .Where(h => h.RentalApplicationId == applicationId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .ToListAsync();

        return View(history);
    }
}