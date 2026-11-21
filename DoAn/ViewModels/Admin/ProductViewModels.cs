
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Falcon.Web.Models.Admin;

public class ProductFormViewModel
{
    public int OriginalStockQuantity { get; set; }
    public int ProductId { get; set; }

    [Display(Name = "Hãng xe")]
    [Range(1, int.MaxValue,
        ErrorMessage = "Vui lòng chọn hãng xe.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên xe.")]
    [StringLength(150)]
    [Display(Name = "Tên xe máy")]
    public string ProductName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(150)]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ chứa chữ thường, số và dấu gạch ngang.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = "";

    [Range(typeof(decimal), "0", "9999999999999.99",
        ErrorMessage = "Giá bán không hợp lệ.")]
    [Display(Name = "Giá bán (VNĐ)")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Phân khối phải lớn hơn 0.")]
    [Display(Name = "Phân khối (cc)")]
    public int Displacement { get; set; }

    [StringLength(100)]
    [Display(Name = "Loại động cơ")]
    public string? EngineType { get; set; }

    [Range(typeof(decimal), "0.01", "99.99",
        ErrorMessage = "Dung tích bình xăng phải từ 0.01 đến 99.99 lít.")]
    [Display(Name = "Dung tích bình xăng (lít)")]
    public decimal? FuelCapacity { get; set; }

    [StringLength(255)]
    [Display(Name = "Các màu xe")]
    public string? ColorOptions { get; set; }

    [Range(0, int.MaxValue,
        ErrorMessage = "Số lượng tồn không được âm.")]
    [Display(Name = "Số lượng tồn kho")]
    public int StockQuantity { get; set; }

    [Display(Name = "Mô tả sản phẩm")]
    public string? Description { get; set; }

    [Display(Name = "Sản phẩm nổi bật")]
    public bool IsFeatured { get; set; }

    [Required]
    [RegularExpression("^(Active|Discontinued)$",
        ErrorMessage = "Trạng thái không hợp lệ.")]
    [Display(Name = "Trạng thái kinh doanh")]
    public string Status { get; set; } = "Active";

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ImageFile { get; set; }

    public string? PrimaryImageUrl { get; set; }
}

public class ProductIndexViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }
    public int AllCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int FeaturedCount { get; set; }

    public int TotalPages =>
        Math.Max(1, (int)Math.Ceiling(
            (double)TotalCount / PageSize));
}
