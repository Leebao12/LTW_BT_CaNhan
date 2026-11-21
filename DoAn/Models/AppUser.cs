namespace Falcon.Web.Models
{
    // Bảng Users
    public class AppUser
    {
        public long UserId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int RoleId { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime? LastLoginAt { get; set; }

        public AppRole Role { get; set; } = null!;
    }

    // Bảng Roles
    public class AppRole
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
    }

}
