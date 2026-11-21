using System.Security.Claims;
using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Controllers;
public class AccountController(FalconDbContext db, IPasswordHasher<AppUser> hasher) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginVm { ReturnUrl = returnUrl });
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == model.Username.Trim());
        var verified = PasswordVerificationResult.Failed;
        if (user != null) { try { verified = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password); } catch (FormatException) { } }
        if (user is null || user.Status != "ACTIVE" || verified == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Thông tin đăng nhập không hợp lệ hoặc tài khoản không hoạt động."); return View(model);
        }
        if (verified == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, model.Password);
        user.LastLoginAt = BusinessClock.Now; await db.SaveChangesAsync();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.Role.RoleName), new Claim("credential_stamp", AccountControllerStamp.For(user.PasswordHash)) }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        HttpContext.Session.Clear();
        if (user.Role.RoleName == "Admin") return RedirectToAction("Index", "Home", new { area = "Admin" });
        if (Url.IsLocalUrl(model.ReturnUrl) && !model.ReturnUrl!.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)) return LocalRedirect(model.ReturnUrl!);
        return user.Role.RoleName == "Admin" ? RedirectToAction("Index", "Home", new { area = "Admin" }) : RedirectToAction("Index", "Home");
    }
    [HttpPost] public async Task<IActionResult> Logout() { HttpContext.Session.Clear(); await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }

    [HttpGet] public IActionResult Register(string? returnUrl) => View(new RegisterVm { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (Request.Form.ContainsKey("RoleId") || Request.Form.ContainsKey("RoleName")) ModelState.AddModelError("", "Không được chỉ định quyền khi đăng ký.");
        if (!ModelState.IsValid) return View(model);
        var role = await db.Roles.SingleOrDefaultAsync(r => r.RoleName == "Customer");
        if (role == null) { ModelState.AddModelError("", "Chưa có vai trò khách hàng. Vui lòng liên hệ cửa hàng."); return View(model); }
        if (await db.Users.AnyAsync(u => u.Username == model.Username.Trim() || u.Email == model.Email.Trim())) { ModelState.AddModelError("", "Tên đăng nhập hoặc email đã được sử dụng."); return View(model); }
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = new AppUser { Username = model.Username.Trim(), Email = model.Email.Trim(), RoleId = role.RoleId, Status = "ACTIVE" };
        u.PasswordHash = hasher.HashPassword(u, model.Password);
        try
        {
            db.Users.Add(u); await db.SaveChangesAsync();
            db.CustomerProfiles.Add(new CustomerProfile { UserId = u.UserId, FullName = model.FullName.Trim(), Phone = model.Phone?.Trim(), Address = model.Address?.Trim() });
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch (DbUpdateException) { await tx.RollbackAsync(); ModelState.AddModelError("", "Không thể đăng ký. Tên đăng nhập hoặc email có thể vừa được sử dụng."); return View(model); }
        TempData["Success"] = "Đăng ký thành công. Hãy đăng nhập để tiếp tục.";
        return RedirectToAction(nameof(Login), new { returnUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : null });
    }
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Profile()
    {
        var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        ViewBag.Email = await db.Users.Where(u => u.UserId == id).Select(u => u.Email).SingleAsync();
        return View(await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == id) ?? new CustomerProfile { FullName = User.Identity!.Name! });
    }
}
