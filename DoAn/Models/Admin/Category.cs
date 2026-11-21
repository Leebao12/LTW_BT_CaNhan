
namespace Falcon.Web.Models.Admin;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public List<Product> Products { get; set; } = new();
}
