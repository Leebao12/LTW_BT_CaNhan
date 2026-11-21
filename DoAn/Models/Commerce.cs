using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Falcon.Web.Models;
[Table("CustomerProfiles", Schema = "dbo")]
public class CustomerProfile
{
    [Key] public long CustomerProfileId { get; set; }
    public long UserId { get; set; }
    [MaxLength(150)] public string FullName { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [Column(TypeName = "date")] public DateTime? DateOfBirth { get; set; }
}
[Table("Showrooms", Schema = "dbo")]
public class Showroom
{
    [Key] public int ShowroomId { get; set; }
    [MaxLength(150)] public string ShowroomName { get; set; } = "";
    [MaxLength(255)] public string Address { get; set; } = "";
    [Column(TypeName = "varchar(20)")] public string? Phone { get; set; }
    public bool IsActive { get; set; }
}
[Table("Coupons", Schema = "dbo")]
public class Coupon
{
    [Key] public int CouponId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = "";
    [MaxLength(255)] public string? Description { get; set; }
    [MaxLength(20)] public string DiscountType { get; set; } = "Percentage";
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal MinOrderAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? MaxDiscountAmount { get; set; }
    [Column(TypeName = "datetime")] public DateTime StartDate { get; set; }
    [Column(TypeName = "datetime")] public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    [Column(TypeName = "datetime")] public DateTime CreatedAt { get; set; }
}
[Table("Orders", Schema = "dbo")]
public class SalesOrder
{
    [Key] public long OrderId { get; set; }
    [MaxLength(30)] public string OrderCode { get; set; } = "";
    public long? UserId { get; set; }
    [MaxLength(150)] public string CustomerName { get; set; } = "";
    [MaxLength(20)] public string CustomerPhone { get; set; } = "";
    [MaxLength(255)] public string CustomerEmail { get; set; } = "";
    [MaxLength(500)] public string? CustomerAddress { get; set; }
    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DepositAmount { get; set; }
    [MaxLength(50)] public string PaymentMethod { get; set; } = "Showroom_Deposit";
    [MaxLength(30)] public string PaymentStatus { get; set; } = "Pending";
    [MaxLength(30)] public string OrderStatus { get; set; } = "Processing";
    public string? Note { get; set; }
    [Column(TypeName = "datetime")] public DateTime CreatedAt { get; set; }
    [Column(TypeName = "datetime")] public DateTime? UpdatedAt { get; set; }
    public List<OrderDetail> Details { get; set; } = [];
}
[Table("OrderDetails", Schema = "dbo")]
public class OrderDetail
{
    [Key] public long OrderDetailId { get; set; }
    public long OrderId { get; set; }
    public int CarId { get; set; }
    [MaxLength(150)] public string CarName { get; set; } = "";
    [MaxLength(50)] public string? ExteriorColor { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)] public decimal TotalPrice { get; set; }
}
[Table("TestDriveBookings", Schema = "dbo")]
public class TestDriveBooking
{
    [Key] public long BookingId { get; set; }
    [MaxLength(30)] public string BookingCode { get; set; } = "";
    public long? UserId { get; set; }
    public int CarId { get; set; }
    public Product Car { get; set; } = null!;
    public int ShowroomId { get; set; }
    public Showroom Showroom { get; set; } = null!;
    [MaxLength(150)] public string FullName { get; set; } = "";
    [MaxLength(20)] public string PhoneNumber { get; set; } = "";
    [MaxLength(255)] public string Email { get; set; } = "";
    [MaxLength(30)] public string? DriverLicenseNumber { get; set; }
    [Column(TypeName = "date")] public DateTime AppointmentDate { get; set; }
    [MaxLength(30)] public string AppointmentTimeSlot { get; set; } = "";
    [MaxLength(30)] public string Status { get; set; } = "Pending";
    public string? StaffNotes { get; set; }
    public string? CustomerNotes { get; set; }
    [Column(TypeName = "datetime")] public DateTime CreatedAt { get; set; }
    [Column(TypeName = "datetime")] public DateTime? UpdatedAt { get; set; }
}
