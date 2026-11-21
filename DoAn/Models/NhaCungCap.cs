using System.ComponentModel.DataAnnotations;
namespace Falcon.Web.Models;
// TV4: form DTO and ADO result; only explicit parameters are written.
public class NhaCungCap {
 public int MaNhaCungCap { get; set; }
 [Required, StringLength(100), Display(Name="Tên nhà cung cấp")]
 public string TenNhaCungCap { get; set; } = "";
 [StringLength(15), RegularExpression(@"^[0-9+ ()-]+$"), Display(Name="Số điện thoại")]
 public string? SoDienThoai { get; set; }
 [EmailAddress, StringLength(50), RegularExpression(@"^[\x20-\x7E]+$"), Display(Name="Email")]
 public string? Email { get; set; }
 [StringLength(255), Display(Name="Địa chỉ")]
 public string? DiaChi { get; set; }
 [Required, StringLength(30), RegularExpression("^(Xe|Phu tung|Ca hai)$"), Display(Name="Loại nhà cung cấp")]
 public string LoaiNhaCungCap { get; set; } = "Xe";
 public bool DangHoatDong { get; set; } = true;
}