using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Falcon.Web.Data;
namespace Falcon.Web.Controllers;
public class HomeController(FalconDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Images).Where(p => p.Status == "Active" && p.Category.IsActive).OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt).Take(6).ToListAsync());
    public IActionResult Pending(string? feature) => feature switch { "Cart" => RedirectToAction("Index", "Cart"), "Checkout" => RedirectToAction("Index", "Checkout"), "Booking" => RedirectToAction("Create", "Booking"), "Tracking" => RedirectToAction("Index", "Orders"), "Profile" => RedirectToAction("Profile", "Account"), _ => RedirectToAction("Index") };
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult Error() { Response.StatusCode = 503; return View(); }
}
