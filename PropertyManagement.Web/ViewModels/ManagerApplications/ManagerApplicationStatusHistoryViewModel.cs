using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.ViewModels.ManagerApplications;

public class ManagerApplicationStatusHistoryViewModel
{
    public RentalApplicationStatus PreviousStatus { get; set; }

    public RentalApplicationStatus NewStatus { get; set; }

    public string ReviewerUserId { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
