using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<FalconDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<NhaCungCapService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.Name = "Falcon.Session"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = async ctx =>
    {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<FalconDbContext>();
        var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(id, out var userId)) { ctx.RejectPrincipal(); return; }
        var user = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null || user.Status != "ACTIVE" || user.Role.RoleName != ctx.Principal?.FindFirstValue(ClaimTypes.Role) ||
           AccountControllerStamp.For(user.PasswordHash) != ctx.Principal?.FindFirstValue("credential_stamp")) ctx.RejectPrincipal();
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.Cookie.Name = "Falcon.Cart"; o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; o.Cookie.SameSite = SameSiteMode.Lax; o.IdleTimeout = TimeSpan.FromMinutes(30); });
builder.Services.AddScoped<CartService>();
builder.Services.AddDataProtection();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
var app = builder.Build();
if (args.Contains("--create-admin"))
{
    using var scope = app.Services.CreateScope();
    await AdminBootstrap.CreateAsync(scope.ServiceProvider); return;
}
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter();
app.UseSession(); app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true) context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthorization();
app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
public partial class Program { }
