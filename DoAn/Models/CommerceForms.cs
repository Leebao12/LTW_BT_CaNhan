using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace Falcon.Web.Models;
public class RegisterVm
{
    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@"[a-zA-Z0-9_.-]+"), Display(Name = "Tên đăng nhập")] public string Username { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu (ít nhất 8 ký tự)")] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu")] public string ConfirmPassword { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Họ và tên")] public string FullName { get; set; } = "";
    [Phone, StringLength(20), Display(Name = "Số điện thoại")] public string? Phone { get; set; }
    [StringLength(500), Display(Name = "Địa chỉ")] public string? Address { get; set; }
    public string? ReturnUrl { get; set; }
}
public class CouponEditVm : IValidatableObject
{
    [StringLength(255), Display(Name = "Mô tả")] public string? Description { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99"), Display(Name = "Giá trị đơn tối thiểu (₫)")] public decimal MinOrderAmount { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99"), Display(Name = "Giảm tối đa (₫), để trống nếu không giới hạn")] public decimal? MaxDiscountAmount { get; set; }
    [DataType(DataType.DateTime), Display(Name = "Bắt đầu (giờ Việt Nam)")] public DateTime StartDate { get; set; } = Services.BusinessClock.Now.Date;
    [DataType(DataType.DateTime), Display(Name = "Kết thúc (giờ Việt Nam)")] public DateTime EndDate { get; set; } = Services.BusinessClock.Now.Date.AddMonths(1).AddDays(1).AddSeconds(-1);
    [Range(1, int.MaxValue), Display(Name = "Giới hạn lượt dùng")] public int UsageLimit { get; set; } = 100;
    [Display(Name = "Kích hoạt mã")] public bool IsActive { get; set; } = true;
    public string? Version { get; set; }
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (StartDate.Year < 1753 || EndDate.Year < 1753 || EndDate < StartDate) yield return new("Thời hạn không hợp lệ; ngày kết thúc phải từ ngày bắt đầu.", [nameof(EndDate)]);
        foreach (var v in new[] { MinOrderAmount, MaxDiscountAmount ?? 0 }) if (decimal.Round(v, 2) != v) { yield return new("Số tiền chỉ được có tối đa 2 chữ số thập phân."); break; }
    }
}
public class CouponCreateVm : CouponEditVm
{
    [Required, StringLength(50), RegularExpression(@"[A-Za-z0-9_-]+"), Display(Name = "Mã giảm giá")] public string Code { get; set; } = "";
    [Required, RegularExpression("Percentage|FixedAmount"), Display(Name = "Loại giảm")] public string DiscountType { get; set; } = "Percentage";
    [Range(typeof(decimal), "0.01", "9999999999999999.99"), Display(Name = "Giá trị giảm (% hoặc ₫)")] public decimal DiscountValue { get; set; }
    public override IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        foreach (var r in base.Validate(c)) yield return r;
        if (DiscountType == "Percentage" && DiscountValue > 100) yield return new("Phần trăm giảm không được quá 100%.", [nameof(DiscountValue)]);
        if (decimal.Round(DiscountValue, 2) != DiscountValue) yield return new("Chỉ nhập tối đa 2 chữ số thập phân.", [nameof(DiscountValue)]);
    }
}
public class CouponListVm
{
    public List<Coupon> Items { get; set; } = [];
    public string? Q { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
    public int Total { get; set; }
    public int Expired { get; set; }
    public int All { get; set; }
    public int Active { get; set; }
    public int Unavailable { get; set; }
    public long Used { get; set; }
}
public record CartLine(int ProductId, int Quantity);
public record CartRow(Product Product, int Quantity) { public decimal Total => Product.Price * Quantity; }
public class CheckoutVm
{
    [Required, StringLength(150), Display(Name = "Họ và tên")] public string CustomerName { get; set; } = "";
    [Required, RegularExpression(@"[0-9+ ()-]{9,20}"), Display(Name = "Số điện thoại")] public string CustomerPhone { get; set; } = "";
    [Required, EmailAddress, StringLength(255), Display(Name = "Email")] public string CustomerEmail { get; set; } = "";
    [StringLength(500), Display(Name = "Địa chỉ")] public string? CustomerAddress { get; set; }
    [StringLength(50), Display(Name = "Mã giảm giá")] public string? CouponCode { get; set; }
    [Required, RegularExpression("BankTransfer_QR|Showroom_Deposit"), Display(Name = "Phương thức thanh toán")] public string PaymentMethod { get; set; } = "Showroom_Deposit";
    [StringLength(2000), Display(Name = "Ghi chú")] public string? Note { get; set; }
    [Required] public string Token { get; set; } = "";
    [ValidateNever] public List<CartRow> Rows { get; set; } = [];
    [ValidateNever] public decimal Discount { get; set; }
    public decimal SubTotal => Rows.Sum(x => x.Total);
    public decimal Total => SubTotal - Discount;
    public decimal Deposit => decimal.Round(Total * 0.1m, 2);
}
public class BookingVm : IValidatableObject
{
    [Range(1, int.MaxValue), Display(Name = "Mẫu xe")] public int CarId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Showroom")] public int ShowroomId { get; set; }
    [Required, StringLength(150), Display(Name = "Họ và tên")] public string FullName { get; set; } = "";
    [Required, RegularExpression(@"[0-9+ ()-]{9,20}"), Display(Name = "Số điện thoại")] public string PhoneNumber { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [StringLength(30), Display(Name = "Số giấy phép lái xe (không bắt buộc)")] public string? DriverLicenseNumber { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày hẹn")] public DateTime AppointmentDate { get; set; } = Services.BusinessClock.Now.Date.AddDays(1);
    [Required, Display(Name = "Khung giờ")] public string AppointmentTimeSlot { get; set; } = "";
    [StringLength(2000), Display(Name = "Ghi chú")] public string? CustomerNotes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (AppointmentDate.Date <= Services.BusinessClock.Now.Date || AppointmentDate.Date > Services.BusinessClock.Now.Date.AddDays(90)) yield return new("Chọn ngày từ ngày mai đến 90 ngày tới.", [nameof(AppointmentDate)]);
        if (!Services.BookingPolicy.Slots.Contains(AppointmentTimeSlot)) yield return new("Chọn khung giờ trong danh sách.", [nameof(AppointmentTimeSlot)]);
    }
}
