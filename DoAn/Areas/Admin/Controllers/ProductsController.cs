
using Falcon.Web.Data;
using Falcon.Web.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Falcon.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly FalconDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        FalconDbContext db,
        IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // =====================================
    // 1. DANH SÁCH, TÌM KIẾM VÀ PHÂN TRANG
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string? status,
        int page = 1)
    {
        search = search?.Trim();
        if (search?.Length > 100)
            search = search[..100];

        if (status != "Active" &&
            status != "Discontinued")
        {
            status = null;
        }

        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.ProductName.Contains(search) ||
                p.Slug.Contains(search) ||
                p.Category.CategoryName.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        if (status != null)
        {
            query = query.Where(p => p.Status == status);
        }

        int totalCount = await query.CountAsync();
        const int pageSize = 10;

        int totalPages = Math.Max(1,
            (int)Math.Ceiling((double)totalCount / pageSize));

        page = Math.Clamp(page, 1, totalPages);

        var products = await query
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .OrderByDescending(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new ProductIndexViewModel
        {
            Products = products,
            Categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync(),

            Search = search,
            CategoryId = categoryId,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,

            AllCount = await _db.Products.CountAsync(),

            OutOfStockCount = await _db.Products
                .CountAsync(p => p.StockQuantity == 0),

            FeaturedCount = await _db.Products
                .CountAsync(p => p.IsFeatured)
        };

        return View(model);
    }

    // =====================================
    // 2. XEM CHI TIẾT SẢN PHẨM
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // =====================================
    // 3. THÊM SẢN PHẨM
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();

        return View(new ProductFormViewModel
        {
            Status = "Active"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductFormViewModel model)
    {
        NormalizeForm(model);

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        await ValidateBusinessRulesAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        string? uploadedPath = await SaveImageAsync(
            model.ImageFile);

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        var product = new Product();
        ApplyForm(model, product);

        if (uploadedPath != null)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = uploadedPath,
                IsPrimary = true
            });
        }

        try
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Thêm sản phẩm xe máy thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            DeleteUploadedFile(uploadedPath);

            ModelState.AddModelError(
                nameof(model.Slug),
                "Slug đã tồn tại. Vui lòng nhập slug khác.");

            await LoadCategoriesAsync();
            return View(model);
        }
        catch (DbUpdateException ex) when (IsForeignKeyError(ex))
        {
            DeleteUploadedFile(uploadedPath);
            ModelState.AddModelError("", "Hãng xe hoặc dữ liệu liên quan vừa thay đổi. Vui lòng kiểm tra lại.");
            await LoadCategoriesAsync();
            return View(model);
        }
        catch
        {
            DeleteUploadedFile(uploadedPath);
            throw;
        }
    }

    // =====================================
    // 4. CHỈNH SỬA SẢN PHẨM
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        await LoadCategoriesAsync();

        return View(ToFormViewModel(product));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ProductFormViewModel model)
    {
        if (id != model.ProductId)
            return BadRequest();

        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        NormalizeForm(model);

        model.PrimaryImageUrl = product.PrimaryImageUrl;
        if (product.StockQuantity != model.OriginalStockQuantity)
            ModelState.AddModelError("", "Tồn kho đã thay đổi từ khi mở trang. Tải lại trang sửa trước khi lưu.");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        await ValidateBusinessRulesAsync(model, id);

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        string? uploadedPath = await SaveImageAsync(
            model.ImageFile);

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        ApplyForm(model, product);

        // Chỉ thay ảnh khi quản trị viên tải ảnh mới.
        if (uploadedPath != null)
        {
            var primaryImage = product.Images
                .FirstOrDefault(i => i.IsPrimary);

            if (primaryImage == null)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = uploadedPath,
                    IsPrimary = true
                });
            }
            else
            {
                primaryImage.ImageUrl = uploadedPath;
            }
        }

        try
        {
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật thông tin xe thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            DeleteUploadedFile(uploadedPath);
            ModelState.AddModelError("", "Xe hoặc tồn kho vừa thay đổi. Vui lòng tải lại trang sửa.");
            await LoadCategoriesAsync();
            return View(model);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            DeleteUploadedFile(uploadedPath);

            ModelState.AddModelError(
                nameof(model.Slug),
                "Slug này đã được sử dụng.");

            await LoadCategoriesAsync();
            return View(model);
        }
        catch (DbUpdateException ex) when (IsForeignKeyError(ex))
        {
            DeleteUploadedFile(uploadedPath);
            ModelState.AddModelError("", "Hãng xe hoặc dữ liệu liên quan vừa thay đổi. Vui lòng kiểm tra lại.");
            await LoadCategoriesAsync();
            return View(model);
        }
        catch
        {
            DeleteUploadedFile(uploadedPath);
            throw;
        }
    }

    // =====================================
    // 5. XÁC NHẬN XÓA SẢN PHẨM
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        ViewBag.HasDependencies =
            await HasRelatedDataAsync(id);

        return View(product);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        if (await HasRelatedDataAsync(id))
        {
            TempData["Error"] =
                "Không thể xóa xe vì đã có đơn hàng, " +
                "lịch lái thử hoặc phiếu nhập liên quan.";

            return RedirectToAction(nameof(Delete),
                new { id });
        }

        try
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa sản phẩm thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (IsForeignKeyError(ex))
        {
            TempData["Error"] =
                "Không thể xóa sản phẩm do còn dữ liệu liên quan.";

            return RedirectToAction(nameof(Delete),
                new { id });
        }
    }

    // =====================================
    // 6. LẤY DANH SÁCH HÃNG XE
    // =====================================

    private async Task LoadCategoriesAsync()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        ViewBag.Categories = new SelectList(
            categories,
            nameof(Category.CategoryId),
            nameof(Category.CategoryName));
    }

    // =====================================
    // 7. KIỂM TRA NGHIỆP VỤ
    // =====================================

    private async Task ValidateBusinessRulesAsync(
        ProductFormViewModel model,
        int? editingId = null)
    {
        bool categoryExists = await _db.Categories
            .AnyAsync(c => c.CategoryId == model.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Hãng xe không tồn tại.");
        }

        bool slugExists = await _db.Products
            .AnyAsync(p =>
                p.Slug == model.Slug &&
                (!editingId.HasValue ||
                 p.ProductId != editingId.Value));

        if (slugExists)
        {
            ModelState.AddModelError(
                nameof(model.Slug),
                "Slug đã tồn tại trong hệ thống.");
        }
    }

    // =====================================
    // 8. CHUẨN HÓA VÀ ÁNH XẠ DỮ LIỆU
    // =====================================

    private static void NormalizeForm(
        ProductFormViewModel model)
    {
        model.ProductName =
            model.ProductName?.Trim() ?? "";

        model.Slug =
            model.Slug?.Trim().ToLowerInvariant() ?? "";

        model.EngineType = model.EngineType?.Trim();
        model.ColorOptions = model.ColorOptions?.Trim();
        model.Description = model.Description?.Trim();
    }

    private static void ApplyForm(
        ProductFormViewModel form,
        Product product)
    {
        product.CategoryId = form.CategoryId;
        product.ProductName = form.ProductName;
        product.Slug = form.Slug;
        product.Price = form.Price;
        product.Displacement = form.Displacement;
        product.EngineType = form.EngineType;
        product.FuelCapacity = form.FuelCapacity;
        product.ColorOptions = form.ColorOptions;
        product.StockQuantity = form.StockQuantity;
        product.Description = form.Description;
        product.IsFeatured = form.IsFeatured;
        product.Status = form.Status;
    }

    private static ProductFormViewModel ToFormViewModel(
        Product p)
    {
        return new ProductFormViewModel
        {
            ProductId = p.ProductId,
            CategoryId = p.CategoryId,
            ProductName = p.ProductName,
            Slug = p.Slug,
            Price = p.Price,
            Displacement = p.Displacement,
            EngineType = p.EngineType,
            FuelCapacity = p.FuelCapacity,
            ColorOptions = p.ColorOptions,
            StockQuantity = p.StockQuantity,
            OriginalStockQuantity = p.StockQuantity,
            Description = p.Description,
            IsFeatured = p.IsFeatured,
            Status = p.Status,
            PrimaryImageUrl = p.PrimaryImageUrl
        };
    }

    // =====================================
    // 9. TẢI ẢNH SẢN PHẨM
    // =====================================

    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        const long maxSize = 5 * 1024 * 1024;

        if (file.Length > maxSize)
        {
            ModelState.AddModelError(
                "ImageFile",
                "Ảnh phải có dung lượng tối đa 5MB.");

            return null;
        }

        string extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        string[] allowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                "ImageFile",
                "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");

            return null;
        }

        // Kiểm tra chữ ký định dạng của tập tin,
        // không chỉ dựa vào phần đuôi tên ảnh.
        await using var input = file.OpenReadStream();

        byte[] header = new byte[12];
        int length = await input.ReadAsync(
            header.AsMemory(0, header.Length));

        bool jpeg = length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF;

        bool png = length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A;

        bool webp = length >= 12 &&
            System.Text.Encoding.ASCII
                .GetString(header, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII
                .GetString(header, 8, 4) == "WEBP";

        bool validFormat = extension switch
        {
            ".jpg" or ".jpeg" => jpeg,
            ".png" => png,
            ".webp" => webp,
            _ => false
        };

        if (!validFormat)
        {
            ModelState.AddModelError(
                "ImageFile",
                "Nội dung tập tin không đúng định dạng ảnh.");

            return null;
        }

        string wwwroot = _environment.WebRootPath
            ?? Path.Combine(
                _environment.ContentRootPath, "wwwroot");

        string folder = Path.Combine(
            wwwroot, "uploads", "products");

        Directory.CreateDirectory(folder);

        string fileName =
            Guid.NewGuid().ToString("N") + extension;

        string fullPath = Path.Combine(folder, fileName);

        await using (var output = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(output);
        }

        return "/uploads/products/" + fileName;
    }

    private void DeleteUploadedFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        if (!imageUrl.StartsWith(
            "/uploads/products/", StringComparison.Ordinal))
            return;

        string wwwroot = _environment.WebRootPath
            ?? Path.Combine(
                _environment.ContentRootPath, "wwwroot");

        string path = Path.Combine(
            wwwroot,
            "uploads",
            "products",
            Path.GetFileName(imageUrl));

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    // =====================================
    // 10. KIỂM TRA RÀNG BUỘC TRƯỚC KHI XÓA
    // =====================================


    private async Task<bool> HasRelatedDataAsync(int id)
    {
        const string sql = @"
        SELECT CAST(
            CASE WHEN
                EXISTS (
                    SELECT 1
                    FROM dbo.OrderDetails
                    WHERE CarId = @Id
                )
                OR EXISTS (
                    SELECT 1
                    FROM dbo.TestDriveBookings
                    WHERE CarId = @Id
                )
                OR EXISTS (
                    SELECT 1
                    FROM dbo.ChiTietDonNhap
                    WHERE product_id = @Id
                )
            THEN 1 ELSE 0 END
        AS INT) AS [Value]";

        int result = await _db.Database
            .SqlQueryRaw<int>(
                sql,
                new SqlParameter("@Id", id))
            .SingleAsync();

        return result == 1;
    }


    // =====================================
    // 11. XỬ LÝ LỖI SQL SERVER
    // =====================================

    private static bool IsDuplicateKey(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627);
    }

    private static bool IsForeignKeyError(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql &&
            sql.Number == 547;
    }
}
