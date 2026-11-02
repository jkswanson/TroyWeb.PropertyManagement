using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.UnitTypes;

public class UnitTypeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
