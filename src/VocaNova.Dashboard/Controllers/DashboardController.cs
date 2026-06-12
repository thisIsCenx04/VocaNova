using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}
