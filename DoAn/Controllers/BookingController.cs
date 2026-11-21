using System.Security.Claims;
using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Controllers;
[Authorize(Roles = "Customer")]
public class BookingController(FalconDbContext db) : Controller
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(int page = 1) { var q = db.TestDriveBookings.AsNoTracking().Include(b => b.Car).Include(b => b.Showroom).Where(b => b.UserId == UserId); var pages = Math.Max(1, (await q.CountAsync() + 19) / 20); page = Math.Clamp(page, 1, pages); ViewBag.Page = page; ViewBag.Pages = pages; return View(await q.OrderByDescending(b => b.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync()); }
    public async Task<IActionResult> Details(long id) { var b = await db.TestDriveBookings.AsNoTracking().Include(b => b.Car).Include(b => b.Showroom).SingleOrDefaultAsync(b => b.BookingId == id && b.UserId == UserId); return b == null ? NotFound() : View(b); }
    public async Task<IActionResult> Create(int carId = 0)
    {
        var profile = await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == UserId); await Options();
        return View(new BookingVm { CarId = carId, FullName = profile?.FullName ?? User.Identity!.Name!, PhoneNumber = profile?.Phone ?? "", Email = await db.Users.Where(u => u.UserId == UserId).Select(u => u.Email).SingleAsync() });
    }
    [HttpPost]
    public async Task<IActionResult> Create(BookingVm m)
    {
        await Options(); if (!ModelState.IsValid) return View(m);
        await using var tx = await CommerceLock.Begin(db, "FALCON:Booking");
        if (!await db.Products.AnyAsync(p => p.ProductId == m.CarId && p.Status == "Active" && p.Category.IsActive)) ModelState.AddModelError("CarId", "Xe không còn nhận lịch lái thử.");
        if (!await db.Showrooms.AnyAsync(s => s.ShowroomId == m.ShowroomId && s.IsActive)) ModelState.AddModelError("ShowroomId", "Showroom không hoạt động.");
        var existing = await db.TestDriveBookings.Where(b => b.AppointmentDate == m.AppointmentDate.Date && (b.Status == "Pending" || b.Status == "Confirmed") && (b.UserId == UserId || b.CarId == m.CarId && b.ShowroomId == m.ShowroomId)).Select(b => b.AppointmentTimeSlot).ToListAsync();
        if (existing.Any(s => BookingPolicy.Overlaps(s, m.AppointmentTimeSlot))) ModelState.AddModelError("AppointmentTimeSlot", "Bạn đã có lịch trùng giờ hoặc xe tại showroom đã được đặt trong khoảng này.");
        if (!ModelState.IsValid) return View(m);
        var b = new TestDriveBooking { BookingCode = "BK" + Guid.NewGuid().ToString("N")[..27], UserId = UserId, CarId = m.CarId, ShowroomId = m.ShowroomId, FullName = m.FullName.Trim(), PhoneNumber = m.PhoneNumber.Trim(), Email = m.Email.Trim(), DriverLicenseNumber = m.DriverLicenseNumber?.Trim(), AppointmentDate = m.AppointmentDate.Date, AppointmentTimeSlot = m.AppointmentTimeSlot, CustomerNotes = m.CustomerNotes?.Trim(), Status = "Pending", CreatedAt = BusinessClock.Now };
        db.TestDriveBookings.Add(b);
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); } catch (DbUpdateException) { ModelState.AddModelError("", "Không thể đặt lịch. Hãy tải lại và kiểm tra thông tin."); return View(m); }
        TempData["Success"] = "Đã tiếp nhận lịch lái thử. Vui lòng chờ cửa hàng xác nhận."; return RedirectToAction(nameof(Details), new { id = b.BookingId });
    }
    [HttpPost]
    public async Task<IActionResult> Cancel(long id)
    {
        await using var tx = await CommerceLock.Begin(db, "FALCON:Booking"); var b = await db.TestDriveBookings.SingleOrDefaultAsync(b => b.BookingId == id && b.UserId == UserId); if (b == null) return NotFound();
        if (!BookingPolicy.CanCancel(b)) { TempData["Error"] = "Chỉ được hủy lịch đang chờ/đã xác nhận và trước giờ hẹn hơn 2 tiếng."; return RedirectToAction(nameof(Details), new { id }); }
        b.Status = "Cancelled"; b.UpdatedAt = BusinessClock.Now; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã hủy lịch hẹn."; return RedirectToAction(nameof(Details), new { id });
    }
    private async Task Options() { ViewBag.Cars = new SelectList(await db.Products.AsNoTracking().Where(p => p.Status == "Active" && p.Category.IsActive).OrderBy(p => p.ProductName).ToListAsync(), "ProductId", "ProductName"); ViewBag.Showrooms = new SelectList(await db.Showrooms.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.ShowroomName).ToListAsync(), "ShowroomId", "ShowroomName"); }
}
