using PropertyManagement.Core.Enums;

namespace PropertyManagement.Core.Entities;

public class RentalApplication
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public Unit Unit { get; set; } = null!;

    public string ApplicantUserId { get; set; } = string.Empty;

    public RentalApplicationStatus Status { get; set; } = RentalApplicationStatus.Draft;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CurrentAddress { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public ICollection<ResidenceHistory> ResidenceHistories { get; set; } = [];

    public ICollection<RentalApplicationStatusHistory> StatusHistory { get; set; } = [];
}
