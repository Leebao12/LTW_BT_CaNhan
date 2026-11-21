using System.Security.Claims;
using System.Security.Cryptography;
using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Controllers;
[Authorize(Roles = "Customer")]
public class CheckoutController(FalconDbContext db, CartService cart, IDataProtectionProvider provider) : Controller
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IDataProtector Protector => provider.CreateProtector("FALCON.Checkout.v1");
    public async Task<IActionResult> Index()
    {
        try
        {
            var rows = await cart.Rows(); CartService.Validate(rows);
            var profile = await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == UserId);
            var email = await db.Users.Where(u => u.UserId == UserId).Select(u => u.Email).SingleAsync();
            var code = "ORD" + Guid.NewGuid().ToString("N")[..26];
            return View(new CheckoutVm { Rows = rows, CustomerName = profile?.FullName ?? User.Identity!.Name!, CustomerPhone = profile?.Phone ?? "", CustomerEmail = email, CustomerAddress = profile?.Address, Token = Protector.Protect($"{UserId}|{code}|{DateTime.UtcNow.Ticks}") });
        }
        catch (BusinessException e) { TempData["Error"] = e.Message; return RedirectToAction("Index", "Cart"); }
    }
    [HttpPost]
    public async Task<IActionResult> Index(CheckoutVm m, string? intent)
    {
        string code;
        try { var parts = Protector.Unprotect(m.Token).Split('|'); if (parts.Length != 3 || parts[0] != UserId.ToString() || DateTime.UtcNow - new DateTime(long.Parse(parts[2]), DateTimeKind.Utc) > TimeSpan.FromHours(2)) throw new FormatException(); code = parts[1]; }
        catch (Exception e) when (e is CryptographicException or FormatException or ArgumentException) { TempData["Error"] = "Phiên đặt mua hết hạn. Vui lòng mở lại trang đặt mua."; return RedirectToAction(nameof(Index)); }
        await using var tx = await CommerceLock.Begin(db);
        var prior = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (prior != null) return RedirectToAction("Details", "Orders", new { id = prior.OrderId });
        try
        {
            m.Rows = await cart.Rows(); CartService.Validate(m.Rows);
            Coupon? coupon = null;
            if (!string.IsNullOrWhiteSpace(m.CouponCode))
            {
                var couponCode = m.CouponCode.Trim().ToUpperInvariant(); coupon = await db.Coupons.SingleOrDefaultAsync(c => c.Code == couponCode);
                if (coupon == null) throw new BusinessException("Mã giảm giá không tồn tại.");
                m.Discount = CouponRules.Discount(coupon, m.SubTotal, BusinessClock.Now);
            }
            if (!ModelState.IsValid || intent == "preview") return View(m);
            var order = new SalesOrder
            {
                OrderCode = code,
                UserId = UserId,
                CustomerName = m.CustomerName.Trim(),
                CustomerPhone = m.CustomerPhone.Trim(),
                CustomerEmail = m.CustomerEmail.Trim(),
                CustomerAddress = m.CustomerAddress?.Trim(),
                CouponId = coupon?.CouponId,
                SubTotal = m.SubTotal,
                DiscountAmount = m.Discount,
                TotalAmount = m.Total,
                DepositAmount = m.Deposit,
                PaymentMethod = m.PaymentMethod,
                PaymentStatus = "Pending",
                OrderStatus = "Processing",
                Note = m.Note?.Trim(),
                CreatedAt = BusinessClock.Now,
                Details = m.Rows.Select(r => new OrderDetail { CarId = r.Product.ProductId, CarName = r.Product.ProductName, Quantity = r.Quantity, UnitPrice = r.Product.Price }).ToList()
            };
            if (coupon != null) coupon.UsedCount++;
            db.Orders.Add(order); await db.SaveChangesAsync(); await tx.CommitAsync(); cart.Clear();
            TempData["Success"] = "Đã tiếp nhận đơn hàng. Cửa hàng sẽ liên hệ xác nhận; chưa thu tiền hoặc giữ xe.";
            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); return View(m); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Không thể lưu đơn. Dữ liệu vừa thay đổi; hãy kiểm tra và thử lại."); return View(m); }
    }
}
