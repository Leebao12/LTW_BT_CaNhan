using System.ComponentModel.DataAnnotations;
namespace Falcon.Web.Models
{
    // Dữ liệu form Thêm tài khoản
    public class UserCreateVm
    {
        [Required(ErrorMessage = "Nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải có từ 8 đến 100 ký tự")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Xác nhận mật khẩu")]
        [Compare(nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = "";

        [Range(1, int.MaxValue,
            ErrorMessage = "Chọn vai trò")]
        public int RoleId { get; set; }
    }

    // Dữ liệu form Sửa tài khoản
    public class UserEditVm
    {
        public long UserId { get; set; }

        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string Email { get; set; } = "";

        [Range(1, int.MaxValue,
            ErrorMessage = "Chọn vai trò")]
        public int RoleId { get; set; }

        [Required]
        [RegularExpression(
            "^(ACTIVE|INACTIVE|LOCKED)$",
            ErrorMessage = "Trạng thái không hợp lệ")]
        public string Status { get; set; } = "ACTIVE";

        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Mật khẩu mới cần ít nhất 8 ký tự")]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword),
            ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string? ConfirmNewPassword { get; set; }
    }

    // Dữ liệu form xác nhận Xóa hoặc Khóa
    public class UserDeleteVm
    {
        public long UserId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public bool HasRelatedData { get; set; }
    }
}
