using Falcon.Web.Data;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class BookingsController(FalconDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, int page = 1) { var query = db.TestDriveBookings.AsNoTracking().Include(b => b.Car).Include(b => b.Showroom).AsQueryable(); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(b => b.BookingCode.Contains(q) || b.FullName.Contains(q) || b.PhoneNumber.Contains(q)); if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status); var pages = Math.Max(1, (await query.CountAsync() + 19) / 20); page = Math.Clamp(page, 1, pages); ViewBag.Q = q; ViewBag.Status = status; ViewBag.Page = page; ViewBag.Pages = pages; return View(await query.OrderByDescending(b => b.AppointmentDate).Skip((page - 1) * 20).Take(20).ToListAsync()); }
    public async Task<IActionResult> Details(long id) { var b = await db.TestDriveBookings.AsNoTracking().Include(b => b.Car).Include(b => b.Showroom).SingleOrDefaultAsync(b => b.BookingId == id); return b == null ? NotFound() : View(b); }
    [HttpPost]
    public async Task<IActionResult> Update(long id, string? status, string? notes, string? expected)
    {
        await using var tx = await CommerceLock.Begin(db, "FALCON:Booking"); var b = await db.TestDriveBookings.SingleOrDefaultAsync(b => b.BookingId == id); if (b == null) return NotFound();
        var start = b.AppointmentDate.Date + (BookingPolicy.Interval(b.AppointmentTimeSlot)?.Start ?? TimeSpan.Zero);
        bool valid = b.Status == expected && ((b.Status == "Pending" && status == "Confirmed" && start > BusinessClock.Now) || ((b.Status is "Pending" or "Confirmed") && status == "Cancelled") || (b.Status == "Confirmed" && (status is "Completed" or "NoShow") && start <= BusinessClock.Now));
        if (!valid || notes?.Length > 2000) { TempData["Error"] = "Chuyển trạng thái không hợp lệ hoặc lịch đã thay đổi. Lịch quá giờ không thể xác nhận mới."; return RedirectToAction(nameof(Details), new { id }); }
        b.Status = status!; b.StaffNotes = notes?.Trim(); b.UpdatedAt = BusinessClock.Now; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã cập nhật lịch hẹn."; return RedirectToAction(nameof(Details), new { id });
    }
}
