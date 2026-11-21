using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Falcon.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class NhaCungCapController : Controller
    {
        private readonly NhaCungCapService _service;
        private readonly ILogger<NhaCungCapController> _logger;

        public NhaCungCapController(NhaCungCapService service, ILogger<NhaCungCapController> logger)
        {
            _service = service; _logger=logger;
        }

        // 1. Danh sách và tìm kiếm
        public async Task<IActionResult> Index(string? keyword)
        {
            var danhSach = await _service.GetAllAsync(keyword);

            ViewBag.Keyword = keyword;

            return View(danhSach);
        }

        // 2. Hiển thị form thêm
        [HttpGet]
        public IActionResult Create()
        {
            return View(new NhaCungCap());
        }

        // 3. Xử lý thêm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhaCungCap model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _service.CreateAsync(model);

                TempData["Success"] = "Thêm nhà cung cấp thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,"Lỗi xử lý nhà cung cấp");
                ModelState.AddModelError("",
                    "Không thể thêm nhà cung cấp. Vui lòng kiểm tra dữ liệu và kết nối cơ sở dữ liệu.");

                return View(model);
            }
        }

        // 4. Chi tiết
        public async Task<IActionResult> Details(int id)
        {
            var nhaCungCap = await _service.GetByIdAsync(id);

            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // 5. Hiển thị form sửa
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhaCungCap = await _service.GetByIdAsync(id);

            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // 6. Xử lý sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhaCungCap model)
        {
            if (id != model.MaNhaCungCap)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                bool updated = await _service.UpdateAsync(model);

                if (!updated)
                {
                    return NotFound();
                }

                TempData["Success"] = "Cập nhật nhà cung cấp thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,"Lỗi xử lý nhà cung cấp");
                ModelState.AddModelError("",
                    "Không thể cập nhật nhà cung cấp. Vui lòng thử lại.");

                return View(model);
            }
        }

        // 7. Xóa hoặc ngừng hoạt động
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                string ketQua = await _service.DeleteAsync(id);

                TempData["Success"] = ketQua;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,"Lỗi xử lý nhà cung cấp");
                TempData["Error"] =
                    "Không thể xử lý nhà cung cấp. Vui lòng kiểm tra lại dữ liệu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}