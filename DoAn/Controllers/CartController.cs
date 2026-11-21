using Falcon.Web.Data;
using Falcon.Web.Models;
using Falcon.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Falcon.Web.Controllers;
[Authorize(Roles = "Customer")]
public class CartController(FalconDbContext db, CartService cart) : Controller
{
    public async Task<IActionResult> Index() { try { return View(await cart.Rows()); } catch (BusinessException e) { TempData["Error"] = e.Message; return View(new List<CartRow>()); } }
    [HttpGet] public async Task<IActionResult> Add(int id, bool buy = false) { var p = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == id && x.Status == "Active"); if (p == null) return NotFound(); ViewBag.Buy = buy; return View(p); }
    [HttpPost, ActionName("Add")]
    public async Task<IActionResult> AddPost(int id, int quantity = 1, bool buy = false)
    {
        var p = await db.Products.Include(p => p.Category).SingleOrDefaultAsync(p => p.ProductId == id); if (p == null) return NotFound();
        var lines = cart.Get(); var old = lines.SingleOrDefault(l => l.ProductId == id); var count = (long)(old?.Quantity ?? 0) + quantity;
        if (!ModelState.IsValid || quantity < 1 || count > 99 || count > p.StockQuantity || p.Status != "Active" || !p.Category.IsActive) { TempData["Error"] = "Xe không khả dụng hoặc số lượng vượt tồn kho (tối đa 99 mỗi mẫu)."; return RedirectToAction(nameof(Index)); }
        lines.RemoveAll(l => l.ProductId == id); lines.Add(new CartLine(id, (int)count)); cart.Save(lines);
        return buy ? RedirectToAction("Index", "Checkout") : RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var lines = cart.Get(); var p = await db.Products.FindAsync(id);
        if (!ModelState.IsValid || p == null || quantity < 1 || quantity > 99 || quantity > p.StockQuantity) { TempData["Error"] = "Số lượng không hợp lệ hoặc vượt tồn kho."; return RedirectToAction(nameof(Index)); }
        if (lines.Any(l => l.ProductId == id)) { lines.RemoveAll(l => l.ProductId == id); lines.Add(new(id, quantity)); cart.Save(lines); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public IActionResult Remove(int id) { var lines = cart.Get(); lines.RemoveAll(l => l.ProductId == id); cart.Save(lines); return RedirectToAction(nameof(Index)); }
    [HttpPost] public IActionResult Clear() { cart.Clear(); return RedirectToAction(nameof(Index)); }
}
