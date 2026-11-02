using PropertyManagement.Core.Enums;

namespace PropertyManagement.Core.Entities;

public class RentalApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public RentalApplicationStatus PreviousStatus { get; set; }

    public RentalApplicationStatus NewStatus { get; set; }

    public string ReviewerUserId { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
