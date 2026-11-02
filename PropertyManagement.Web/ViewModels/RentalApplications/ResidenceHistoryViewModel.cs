using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ResidenceHistoryViewModel
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move In Date")]
    public DateTime? MoveInDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Move Out Date")]
    public DateTime? MoveOutDate { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Landlord Name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Landlord Phone")]
    public string LandlordPhone { get; set; } = string.Empty;
}
