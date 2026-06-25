using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Dashboard.Controllers;

// Trang chủ dashboard. F055 chỉ dựng layout + landing; nội dung overview (cards/charts) sẽ làm ở F056.
public sealed class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
