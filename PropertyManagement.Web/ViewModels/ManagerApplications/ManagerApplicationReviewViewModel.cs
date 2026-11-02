using PropertyManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.ManagerApplications;

public class ManagerApplicationReviewViewModel
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public RentalApplicationStatus Decision { get; set; }

    [DataType(DataType.Date)]
    public DateTime? LeaseStartDate { get; set; }

    [StringLength(2000)]
    public string? Comments { get; set; }
}
