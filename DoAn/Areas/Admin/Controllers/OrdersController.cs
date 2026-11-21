using Falcon.Web.Data;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class OrdersController(FalconDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1) { var query = db.Orders.AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.OrderCode.Contains(q) || o.CustomerName.Contains(q)); var pages = Math.Max(1, (await query.CountAsync() + 19) / 20); page = Math.Clamp(page, 1, pages); ViewBag.Q = q; ViewBag.Page = page; ViewBag.Pages = pages; return View(await query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync()); }
    public async Task<IActionResult> Details(long id) { var o = await db.Orders.AsNoTracking().Include(o => o.Details).Include(o => o.Coupon).SingleOrDefaultAsync(o => o.OrderId == id); return o == null ? NotFound() : View(o); }
    [HttpPost]
    public async Task<IActionResult> Update(long id, string? status, string? payment, string? expected, string? expectedPayment)
    {
        await using var tx = await CommerceLock.Begin(db); var o = await db.Orders.Include(o => o.Coupon).Include(o => o.Details).SingleOrDefaultAsync(o => o.OrderId == id); if (o == null) return NotFound();
        bool transition = (o.OrderStatus, status) switch { ("Processing", "Confirmed") => true, ("Confirmed", "Delivering") => true, ("Delivering", "Completed") => true, _ => false };
        bool cancel = status == "Cancelled" && (o.OrderStatus is "Processing" or "Confirmed" or "Delivering") && o.PaymentStatus is "Pending" or "Failed";
        bool paymentOk = (o.PaymentStatus, payment) switch { ("Paid", "Paid") => true, ("Deposited", "Deposited" or "Paid") => true, ("Pending" or "Failed", "Pending" or "Failed" or "Deposited" or "Paid") => true, _ => false };
        bool same = status == o.OrderStatus && o.OrderStatus is not ("Completed" or "Cancelled");
        if (o.OrderStatus != expected || o.PaymentStatus != expectedPayment || !paymentOk || !(transition || cancel || same) || status == "Completed" && payment != "Paid" || cancel && payment != o.PaymentStatus) { TempData["Error"] = "Thao tác không hợp lệ hoặc đơn vừa thay đổi. Đơn đã thu tiền cần xử lý hoàn tiền ngoài hệ thống trước khi hủy; không được tự xác nhận thanh toán."; return RedirectToAction(nameof(Details), new { id }); }
        if (status == "Completed")
        {
            foreach (var group in o.Details.GroupBy(d => d.CarId).OrderBy(g => g.Key))
            {
                var count = group.Sum(d => d.Quantity);
                // Conditional SQL update avoids a lost stock update and checks stock at the write boundary.
                var changed = await db.Products.Where(p => p.ProductId == group.Key && p.StockQuantity >= count).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - count));
                if (changed != 1) { TempData["Error"] = "Tồn kho không đủ để hoàn tất đơn. Chưa trừ kho hoặc cập nhật trạng thái."; return RedirectToAction(nameof(Details), new { id }); }
            }
        }
        if (cancel && o.Coupon != null && o.Coupon.UsedCount > 0) o.Coupon.UsedCount--;
        o.OrderStatus = status!; o.PaymentStatus = payment!; o.UpdatedAt = BusinessClock.Now; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã cập nhật đơn hàng."; return RedirectToAction(nameof(Details), new { id });
    }
}
