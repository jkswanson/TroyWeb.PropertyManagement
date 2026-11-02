using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.Properties;

public class PropertyFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;
}
