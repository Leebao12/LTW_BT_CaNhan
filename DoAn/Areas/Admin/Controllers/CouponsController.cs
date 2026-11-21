using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class CouponsController(FalconDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? type, string? status, int page = 1)
    {
        var now = BusinessClock.Now; var all = db.Coupons.AsNoTracking(); var query = all;
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Code.Contains(q.Trim()) || c.Description != null && c.Description.Contains(q.Trim()));
        if (type is "Percentage" or "FixedAmount") query = query.Where(c => c.DiscountType == type);
        query = status switch
        {
            "inactive" => query.Where(c => !c.IsActive),
            "expired" => query.Where(c => c.IsActive && c.EndDate < now),
            "depleted" => query.Where(c => c.IsActive && c.EndDate >= now && c.UsedCount >= c.UsageLimit),
            "scheduled" => query.Where(c => c.IsActive && c.EndDate >= now && c.UsedCount < c.UsageLimit && c.StartDate > now),
            "active" => query.Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now && c.UsedCount < c.UsageLimit),
            _ => query
        };
        var total = await query.CountAsync(); var pages = Math.Max(1, (total + 9) / 10); page = Math.Clamp(page, 1, pages);
        var count = await all.CountAsync(); var active = await all.CountAsync(c => c.IsActive && c.StartDate <= now && c.EndDate >= now && c.UsedCount < c.UsageLimit);
        return View(new CouponListVm { Items = await query.OrderByDescending(c => c.CouponId).Skip((page - 1) * 10).Take(10).ToListAsync(), Q = q, Type = type, Status = status, Page = page, Pages = pages, Total = total, All = count, Expired = await all.CountAsync(c => c.EndDate < now), Active = active, Unavailable = count - active, Used = await all.SumAsync(c => (long)c.UsedCount) });
    }
    public IActionResult Create() => View(new CouponCreateVm());
    [HttpPost]
    public async Task<IActionResult> Create(CouponCreateVm m)
    {
        if (!ModelState.IsValid) return View(m);
        await using var tx = await CommerceLock.Begin(db);
        var code = m.Code.Trim().ToUpperInvariant();
        if (await db.Coupons.AnyAsync(c => c.Code == code)) { ModelState.AddModelError("Code", "Mã đã tồn tại."); return View(m); }
        var c = new Coupon { Code = code, DiscountType = m.DiscountType, DiscountValue = m.DiscountValue, CreatedAt = BusinessClock.Now, UsedCount = 0 }; Apply(c, m); db.Coupons.Add(c);
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Không thể lưu mã. Hãy kiểm tra mã trùng và các điều kiện."); return View(m); }
        TempData["Success"] = "Đã tạo mã giảm giá."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var c = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound();
        ViewBag.Coupon = c; return View(new CouponEditVm { Description = c.Description, MinOrderAmount = c.MinOrderAmount, MaxDiscountAmount = c.MaxDiscountAmount, StartDate = c.StartDate, EndDate = c.EndDate, UsageLimit = c.UsageLimit, IsActive = c.IsActive, Version = CouponRules.Version(c) });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CouponEditVm m)
    {
        await using var tx = await CommerceLock.Begin(db);
        var c = await db.Coupons.SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound(); ViewBag.Coupon = c;
        if (m.Version != CouponRules.Version(c)) ModelState.AddModelError("", "Mã vừa thay đổi hoặc vừa được sử dụng. Mở lại trang sửa để lấy dữ liệu mới.");
        if (m.UsageLimit < c.UsedCount) ModelState.AddModelError("UsageLimit", $"Giới hạn không được nhỏ hơn {c.UsedCount} lượt đã dùng.");
        if (!ModelState.IsValid) return View(m);
        Apply(c, m);
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); } catch (DbUpdateException) { ModelState.AddModelError("", "Không thể lưu thay đổi. Hãy tải lại và kiểm tra dữ liệu."); return View(m); }
        TempData["Success"] = "Đã cập nhật mã giảm giá."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Details(int id)
    {
        var c = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound();
        ViewBag.Orders = await db.Orders.AsNoTracking().Where(o => o.CouponId == id).OrderByDescending(o => o.CreatedAt).ToListAsync(); return View(c);
    }
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound();
        ViewBag.Protected = c.UsedCount > 0 || await db.Orders.AnyAsync(o => o.CouponId == id); return View(c);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string? version)
    {
        await using var tx = await CommerceLock.Begin(db); var c = await db.Coupons.SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound();
        if (version != CouponRules.Version(c)) { TempData["Error"] = "Mã đã thay đổi. Vui lòng kiểm tra lại."; return RedirectToAction(nameof(Delete), new { id }); }
        if (c.UsedCount > 0 || await db.Orders.AnyAsync(o => o.CouponId == id)) { TempData["Error"] = "Mã đã có lịch sử sử dụng; hãy vô hiệu hóa để giữ lịch sử đơn hàng."; return RedirectToAction(nameof(Delete), new { id }); }
        db.Coupons.Remove(c); try { await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã xóa mã chưa sử dụng."; } catch (DbUpdateException) { TempData["Error"] = "Không thể xóa vì mã đang có dữ liệu liên quan."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Toggle(int id, string? version, bool active)
    {
        await using var tx = await CommerceLock.Begin(db); var c = await db.Coupons.SingleOrDefaultAsync(c => c.CouponId == id); if (c == null) return NotFound();
        if (version != CouponRules.Version(c)) { TempData["Error"] = "Mã đã thay đổi. Vui lòng tải lại trước khi đổi trạng thái."; return RedirectToAction(nameof(Index)); }
        c.IsActive = active; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã cập nhật trạng thái mã."; return RedirectToAction(nameof(Index));
    }
    private static void Apply(Coupon c, CouponEditVm m) { c.Description = m.Description?.Trim(); c.MinOrderAmount = m.MinOrderAmount; c.MaxDiscountAmount = c.DiscountType == "Percentage" ? m.MaxDiscountAmount : null; c.StartDate = m.StartDate; c.EndDate = m.EndDate; c.UsageLimit = m.UsageLimit; c.IsActive = m.IsActive; }
}
