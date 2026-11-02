using Microsoft.AspNetCore.Mvc.Rendering;

namespace PropertyManagement.Web.ViewModels.Units;

public class ApplicantUnitSearchViewModel
{
    public int? PropertyId { get; set; }
    public int? MinimumBedrooms { get; set; }

    public IEnumerable<SelectListItem> Properties { get; set; } = [];
    public IEnumerable<AvailableUnitViewModel> Units { get; set; } = [];
}
