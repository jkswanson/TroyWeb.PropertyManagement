using PropertyManagement.Core.Enums;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Web.ViewModels.ManagerApplications;

public class ManagerApplicationDetailsViewModel
{
    public int Id { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    public RentalApplicationStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public RentalApplicationWizardViewModel Application { get; set; } = new();

    public List<ManagerApplicationStatusHistoryViewModel> StatusHistory { get; set; } = [];
}
