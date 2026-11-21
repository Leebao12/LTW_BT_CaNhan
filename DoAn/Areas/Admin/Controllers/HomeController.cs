using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Falcon.Web.Services;
namespace Falcon.Web.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Roles="Admin")]
public class HomeController(DashboardService service) : Controller {
 public async Task<IActionResult> Index() => View(await service.GetDashboardDataAsync());
}
