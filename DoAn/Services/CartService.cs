using System.Security.Claims;
using System.Text.Json;
using Falcon.Web.Data;
using Falcon.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Services;
public class CartService(IHttpContextAccessor accessor, FalconDbContext db)
{
    private ISession Session => accessor.HttpContext!.Session;
    private string Key => "cart:" + accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public List<CartLine> Get() { var json = Session.GetString(Key); return json == null ? [] : JsonSerializer.Deserialize<List<CartLine>>(json) ?? []; }
    public void Save(List<CartLine> lines) => Session.SetString(Key, JsonSerializer.Serialize(lines));
    public void Clear() => Session.Remove(Key);
    public async Task<List<CartRow>> Rows()
    {
        var lines = Get(); var ids = lines.Select(l => l.ProductId).ToArray();
        var products = await db.Products.Include(p => p.Category).Include(p => p.Images).Where(p => ids.Contains(p.ProductId)).ToListAsync();
        if (products.Count != lines.Count) throw new BusinessException("Một xe đã bị xóa. Hãy làm trống giỏ hàng rồi chọn lại.");
        return lines.Select(l => new CartRow(products.Single(p => p.ProductId == l.ProductId), l.Quantity)).ToList();
    }
    public static void Validate(List<CartRow> rows)
    {
        if (rows.Count == 0) throw new BusinessException("Giỏ hàng đang trống.");
        foreach (var r in rows) if (r.Quantity < 1 || r.Quantity > 99 || r.Product.Status != "Active" || !r.Product.Category.IsActive || r.Quantity > r.Product.StockQuantity) throw new BusinessException($"{r.Product.ProductName}: số lượng vượt tồn kho hoặc xe ngừng kinh doanh. Hãy cập nhật giỏ hàng.");
    }
}
