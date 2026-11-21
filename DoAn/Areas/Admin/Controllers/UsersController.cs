using Falcon.Web.Data;
using Falcon.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Falcon.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly FalconDbContext _db;
        private readonly ILogger<UsersController> _logger;
        private readonly IPasswordHasher<AppUser> _hasher;

        public UsersController(
            FalconDbContext db,
            IPasswordHasher<AppUser> hasher, ILogger<UsersController> logger)
        {
            _db = db;
            _hasher = hasher; _logger = logger;
        }

        // ======================================
        // READ - DANH SÁCH + TÌM KIẾM
        // GET: /Admin/Users
        // ======================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? keyword, int page = 1)
        {
            var query = _db.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string k = keyword.Trim();

                query = query.Where(u =>
                    u.Username.Contains(k) ||
                    u.Email.Contains(k));
            }

            ViewBag.Keyword = keyword;
            var total = await query.CountAsync();
            var pages = Math.Max(1, (total + 19) / 20); page = Math.Clamp(page, 1, pages);
            ViewBag.Page = page; ViewBag.Pages = pages; ViewBag.Total = total;

            var users = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * 20).Take(20).ToListAsync();

            return View(users);
        }

        // ======================================
        // READ - XEM CHI TIẾT
        // GET: /Admin/Users/Details/1
        // ======================================
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // ======================================
        // CREATE - MỞ FORM THÊM
        // ======================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadRoles();

            return View(new UserCreateVm());
        }

        // ======================================
        // CREATE - LƯU TÀI KHOẢN MỚI
        // ======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            UserCreateVm model)
        {
            if (!ModelState.IsValid)
            {
                await LoadRoles(model.RoleId);
                return View(model);
            }

            string username = model.Username.Trim();
            string email = model.Email.Trim();

            bool trungUsername = await _db.Users
                .AnyAsync(u => u.Username == username);

            if (trungUsername)
            {
                ModelState.AddModelError(
                    nameof(model.Username),
                    "Tên đăng nhập đã tồn tại");
            }

            bool trungEmail = await _db.Users
                .AnyAsync(u => u.Email == email);

            if (trungEmail)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email đã được sử dụng");
            }

            bool roleExists = await _db.Roles
                .AnyAsync(r => r.RoleId == model.RoleId && r.RoleName == "Customer");

            if (!roleExists)
            {
                ModelState.AddModelError(
                    nameof(model.RoleId),
                    "Vai trò không hợp lệ");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Mật khẩu không được để trống");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoles(model.RoleId);
                return View(model);
            }

            var user = new AppUser
            {
                Username = username,
                Email = email,
                RoleId = model.RoleId,
                Status = "ACTIVE"
            };

            // Băm mật khẩu, không lưu mật khẩu gốc
            user.PasswordHash =
                _hasher.HashPassword(user, model.Password);

            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi ghi tài khoản");
                ModelState.AddModelError(
                    "",
                    "Không thể tạo tài khoản. " +
                    "Kiểm tra tên đăng nhập hoặc email.");

                await LoadRoles(model.RoleId);
                return View(model);
            }

            TempData["Success"] =
                "Thêm tài khoản thành công!";

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // UPDATE - MỞ FORM SỬA
        // ======================================
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (await _db.Roles.AnyAsync(r => r.RoleId == user.RoleId && r.RoleName == "Admin")) return Forbid();
            var model = new UserEditVm
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RoleId = user.RoleId,
                Status = user.Status
            };

            await LoadRoles(model.RoleId);

            return View(model);
        }

        // ======================================
        // UPDATE - LƯU THAY ĐỔI
        // ======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id, UserEditVm model)
        {
            if (await _db.Users.AnyAsync(u => u.UserId == id && u.Role.RoleName == "Admin")) return Forbid();
            model.Username = await _db.Users.Where(u => u.UserId == id).Select(u => u.Username).FirstOrDefaultAsync();
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString() && (model.Status != "ACTIVE" || !await _db.Roles.AnyAsync(r => r.RoleId == model.RoleId && r.RoleName == "Admin")))
                ModelState.AddModelError("", "Không được tự khóa hoặc hạ quyền tài khoản đang đăng nhập.");
            if (id != model.UserId)
                return BadRequest();

            if (model.NewPassword != null &&
                string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    "Mật khẩu mới không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoles(model.RoleId);
                return View(model);
            }

            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            string email = model.Email.Trim();

            bool emailExists = await _db.Users
                .AnyAsync(u =>
                    u.Email == email &&
                    u.UserId != id);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email đã được sử dụng");
            }

            bool roleExists = await _db.Roles
                .AnyAsync(r => r.RoleId == model.RoleId && r.RoleName == "Customer");

            if (!roleExists)
            {
                ModelState.AddModelError(
                    nameof(model.RoleId),
                    "Vai trò không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoles(model.RoleId);
                return View(model);
            }

            // Cập nhật những trường được phép sửa
            user.Email = email;
            user.RoleId = model.RoleId;
            user.Status = model.Status;

            // Chỉ đổi mật khẩu nếu người dùng nhập mới
            if (!string.IsNullOrWhiteSpace(
                model.NewPassword))
            {
                user.PasswordHash =
                    _hasher.HashPassword(
                        user, model.NewPassword);
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi ghi tài khoản");
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật tài khoản.");

                await LoadRoles(model.RoleId);
                return View(model);
            }

            TempData["Success"] =
                "Cập nhật tài khoản thành công!";

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // DELETE - MỞ TRANG XÁC NHẬN
        // ======================================
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == id);

            if (user == null)
                return NotFound();

            if (await _db.Roles.AnyAsync(r => r.RoleId == user.RoleId && r.RoleName == "Admin")) return Forbid();
            bool hasRelated = await HasRelatedData(id);

            var model = new UserDeleteVm
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Status = user.Status,
                HasRelatedData = hasRelated
            };

            return View(model);
        }

        // ======================================
        // DELETE - XÓA HOẶC KHÓA TÀI KHOẢN
        // ======================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            long id)
        {
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString()) { TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập."; return RedirectToAction(nameof(Index)); }
            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (await _db.Roles.AnyAsync(r => r.RoleId == user.RoleId && r.RoleName == "Admin")) return Forbid();
            bool hasRelated = await HasRelatedData(id);

            if (hasRelated)
            {
                // Đã có giao dịch/hồ sơ => khóa
                user.Status = "LOCKED";

                TempData["Success"] =
                    "Tài khoản có dữ liệu liên quan. " +
                    "Đã chuyển sang trạng thái LOCKED.";
            }
            else
            {
                // Không có dữ liệu liên quan => xóa
                _db.Users.Remove(user);

                TempData["Success"] =
                    "Đã xóa tài khoản thành công!";
            }

            try
            {
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi ghi tài khoản");
                TempData.Remove("Success");
                TempData["Error"] =
                    "Không thể xóa/khóa tài khoản. " +
                    "Có thể dữ liệu liên quan vừa thay đổi. " +
                    "Vui lòng kiểm tra lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // HÀM KIỂM TRA DỮ LIỆU LIÊN QUAN
        // ======================================
        private async Task<bool> HasRelatedData(
            long userId)
        {
            bool hasProfile =
                await _db.CustomerProfiles
                    .AnyAsync(p => p.UserId == userId);

            bool hasOrders =
                await _db.Orders
                    .AnyAsync(o => o.UserId == userId);

            bool hasBookings =
                await _db.TestDriveBookings
                    .AnyAsync(b => b.UserId == userId);

            return hasProfile || hasOrders || hasBookings;
        }

        // ======================================
        // HÀM LOAD DANH SÁCH VAI TRÒ
        // ======================================
        private async Task LoadRoles(
            int selectedId = 0)
        {
            var roles = await _db.Roles
                .AsNoTracking().Where(r => r.RoleName == "Customer")
                .OrderBy(r => r.RoleId)
                .ToListAsync();

            ViewBag.RoleOptions = new SelectList(
                roles,
                "RoleId",
                "RoleName",
                selectedId);
        }
    }
}