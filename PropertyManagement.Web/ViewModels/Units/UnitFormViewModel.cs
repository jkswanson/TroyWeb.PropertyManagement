using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.Units;

public class UnitFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Property")]
    public int? PropertyId { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Unit Number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    [Display(Name = "Monthly Rent")]
    public decimal MonthlyRent { get; set; }

    [Required]
    [Display(Name = "Unit Type")]
    public int? UnitTypeId { get; set; }

    public IEnumerable<SelectListItem> Properties { get; set; } = [];

    public IEnumerable<SelectListItem> UnitTypes { get; set; } = [];
}
