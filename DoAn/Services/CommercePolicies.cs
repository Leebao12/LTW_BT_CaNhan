using Falcon.Web.Models;
using Falcon.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Falcon.Web.Services;
public static class BusinessClock { public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified); }
public class BusinessException(string message) : Exception(message);
public static class CouponRules
{
    public static string State(Coupon c, DateTime now) => !c.IsActive ? "inactive" : now > c.EndDate ? "expired" : c.UsedCount >= c.UsageLimit ? "depleted" : now < c.StartDate ? "scheduled" : "active";
    public static string Label(string state) => state switch { "inactive" => "Vô hiệu hóa", "expired" => "Hết hạn", "depleted" => "Hết lượt", "scheduled" => "Chưa bắt đầu", _ => "Hoạt động" };
    public static decimal Discount(Coupon c, decimal subtotal, DateTime now)
    {
        if (State(c, now) != "active") throw new BusinessException("Mã giảm giá chưa có hiệu lực, hết hạn, đã tắt hoặc hết lượt.");
        if (subtotal < c.MinOrderAmount) throw new BusinessException($"Đơn hàng phải từ {c.MinOrderAmount:N0} ₫ để áp dụng mã.");
        var amount = c.DiscountType == "Percentage" ? subtotal * c.DiscountValue / 100 : c.DiscountValue;
        if (c.DiscountType == "Percentage" && c.MaxDiscountAmount.HasValue) amount = Math.Min(amount, c.MaxDiscountAmount.Value);
        return decimal.Round(Math.Clamp(amount, 0, subtotal), 2);
    }
    public static string Version(Coupon c) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(c))));
}
public static class BookingPolicy
{
    public static readonly string[] Slots = ["09:00 - 10:30", "14:30 - 16:00", "16:30 - 18:00"];
    public static (TimeSpan Start, TimeSpan End)? Interval(string slot)
    {
        var parts = slot.Split('-');
        return parts.Length == 2 && TimeSpan.TryParse(parts[0].Trim(), out var a) && TimeSpan.TryParse(parts[1].Trim(), out var b) && b > a ? (a, b) : null;
    }
    public static bool Overlaps(string a, string b) { var x = Interval(a); var y = Interval(b); return x == null || y == null || x.Value.Start < y.Value.End && y.Value.Start < x.Value.End; }
    public static bool CanCancel(TestDriveBooking b) => b.Status is "Pending" or "Confirmed" && Interval(b.AppointmentTimeSlot) is { } t && b.AppointmentDate.Date + t.Start > BusinessClock.Now.AddHours(2);
}
public static class CommerceLock
{
    // A transaction-owned SQL Server application lock serializes writes across app instances.
    public static async Task<IDbContextTransaction> Begin(FalconDbContext db, string resource = "FALCON:Commerce")
    {
        var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Operation busy. Please retry.', 1;");
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
}
public static class StatusText
{
    public static string Of(string s) => s switch { "Processing" => "Chờ xử lý", "Pending" => "Chờ xác nhận", "Confirmed" => "Đã xác nhận", "Delivering" => "Đang giao", "Completed" => "Hoàn tất", "Cancelled" => "Đã hủy", "NoShow" => "Vắng mặt", "Paid" => "Đã thanh toán", "Deposited" => "Đã đặt cọc", "Failed" => "Thanh toán thất bại", _ => s };
}
