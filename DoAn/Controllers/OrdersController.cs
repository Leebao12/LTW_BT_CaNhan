using System.Security.Claims;
using Falcon.Web.Data;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Controllers;
[Authorize(Roles = "Customer")]
public class OrdersController(FalconDbContext db) : Controller
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(string? q, int page = 1) { var query = db.Orders.AsNoTracking().Where(o => o.UserId == UserId); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.OrderCode.Contains(q.Trim())); ViewBag.Q = q; var pages = Math.Max(1, (await query.CountAsync() + 19) / 20); page = Math.Clamp(page, 1, pages); ViewBag.Page = page; ViewBag.Pages = pages; return View(await query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync()); }
    public async Task<IActionResult> Details(long id) { var o = await db.Orders.AsNoTracking().Include(o => o.Details).Include(o => o.Coupon).SingleOrDefaultAsync(o => o.OrderId == id && o.UserId == UserId); return o == null ? NotFound() : View(o); }
    [HttpPost]
    public async Task<IActionResult> Cancel(long id)
    {
        await using var tx = await CommerceLock.Begin(db); var o = await db.Orders.Include(o => o.Coupon).SingleOrDefaultAsync(o => o.OrderId == id && o.UserId == UserId); if (o == null) return NotFound();
        if (o.OrderStatus != "Processing" || o.PaymentStatus != "Pending") { TempData["Error"] = "Chỉ được tự hủy đơn chưa xác nhận và chưa thanh toán. Hãy liên hệ cửa hàng."; return RedirectToAction(nameof(Details), new { id }); }
        o.OrderStatus = "Cancelled"; o.UpdatedAt = BusinessClock.Now; if (o.Coupon != null && o.Coupon.UsedCount > 0) o.Coupon.UsedCount--;
        await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã hủy đơn. Lượt dùng ưu đãi (nếu có) đã được hoàn lại."; return RedirectToAction(nameof(Details), new { id });
    }
}
