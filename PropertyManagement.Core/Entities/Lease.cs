namespace PropertyManagement.Core.Entities;

public class Lease
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public int UnitId { get; set; }

    public Unit Unit { get; set; } = null!;

    public string ApplicantUserId { get; set; } = string.Empty;

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
