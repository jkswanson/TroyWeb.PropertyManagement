using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.ViewModels.Errors;
using PropertyManagement.Web.ViewModels.Home;
using System.Diagnostics;

namespace PropertyManagement.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            Title = "Property Management",
            Description = "Browse available apartments and manage rental applications."
        };

        return View(model);
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
