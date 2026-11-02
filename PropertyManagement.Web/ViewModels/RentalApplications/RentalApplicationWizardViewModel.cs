using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class RentalApplicationWizardViewModel
{
    public int Id { get; set; }

    public bool CanEdit { get; set; }

    public int UnitId { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    [Display(Name = "Current Address")]
    public string CurrentAddress { get; set; } = string.Empty;

    public List<ResidenceHistoryViewModel> Residences { get; set; } = [];
}
