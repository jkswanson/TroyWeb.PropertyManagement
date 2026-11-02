using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class RentalApplicationListViewModel
{
    public int Id { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    public RentalApplicationStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public bool CanEdit =>
        Status == RentalApplicationStatus.Draft ||
        Status == RentalApplicationStatus.Returned;
}
