using Falcon.Web.Data;
using Falcon.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
namespace Falcon.Web.Services;
public static class AdminBootstrap
{
    public static async Task CreateAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<FalconDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.Users.AnyAsync(u => u.Role.RoleName == "Admin")) throw new InvalidOperationException("Đã có Admin; không được tạo Admin thứ hai.");
        Console.Write("Tên đăng nhập (3–50 ký tự): "); var name = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Email: "); var email = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Mật khẩu (12–100 ký tự, không hiển thị): "); var password = ReadSecret();
        if (name.Length < 3 || name.Length > 50 || email.Length > 255 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || password.Length < 12 || password.Length > 100)
            throw new InvalidOperationException("Thông tin không hợp lệ. Không tạo tài khoản.");
        var role = await db.Roles.SingleAsync(r => r.RoleName == "Admin");
        var user = new AppUser { Username = name, Email = email, RoleId = role.RoleId, Status = "ACTIVE" };
        user.PasswordHash = services.GetRequiredService<IPasswordHasher<AppUser>>().HashPassword(user, password);
        db.Users.Add(user); await db.SaveChangesAsync(); await tx.CommitAsync(); Console.WriteLine("Đã tạo Admin. Chạy website và đăng nhập bằng thông tin vừa nhập.");
    }
    private static string ReadSecret()
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Mở terminal tương tác để nhập mật khẩu; không truyền mật khẩu qua tham số.");
        var chars = new System.Text.StringBuilder(); ConsoleKeyInfo k;
        while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter) { if (k.Key == ConsoleKey.Backspace && chars.Length > 0) chars.Length--; else if (!char.IsControl(k.KeyChar) && chars.Length < 101) chars.Append(k.KeyChar); }
        Console.WriteLine(); return chars.ToString();
    }
}
public static class AccountControllerStamp
{
    public static string For(string hash) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hash)));
}
