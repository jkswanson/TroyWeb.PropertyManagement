using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.ViewModels.ManagerApplications;

public class ManagerApplicationIndexViewModel
{
    public RentalApplicationStatus? Status { get; set; }

    public int? PropertyId { get; set; }

    public List<SelectListItem> Properties { get; set; } = [];

    public List<ManagerApplicationListViewModel> Applications { get; set; } = [];
}
