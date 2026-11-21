
namespace Falcon.Web.Models.Admin;

public class ProductImage
{
    public int ImageId { get; set; }
    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = "";
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
}
