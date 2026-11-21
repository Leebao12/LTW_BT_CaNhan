
namespace Falcon.Web.Models.Admin;

public class Product
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }

    public string ProductName { get; set; } = "";
    public string Slug { get; set; } = "";

    public decimal Price { get; set; }
    public int Displacement { get; set; }

    public string? EngineType { get; set; }
    public decimal? FuelCapacity { get; set; }
    public string? ColorOptions { get; set; }

    public int StockQuantity { get; set; }
    public string? Description { get; set; }

    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public List<ProductImage> Images { get; set; } = new();

    public string? PrimaryImageUrl =>
        Images.FirstOrDefault(x => x.IsPrimary)?.ImageUrl;
}
