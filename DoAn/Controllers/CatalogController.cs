using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Falcon.Web.Data;
using Falcon.Web.Models;
namespace Falcon.Web.Controllers;
public class CatalogController(FalconDbContext db) : Controller {
 public async Task<IActionResult> Index(string? search,int? categoryId,string? price,string? cc,string? sort,int page=1) {
  search=search?.Trim(); if(search?.Length>100) search=search[..100];
  var q=db.Products.AsNoTracking().Include(p=>p.Category).Include(p=>p.Images).Where(p=>p.Status=="Active" && p.Category.IsActive);
  if(!string.IsNullOrEmpty(search)) q=q.Where(p=>p.ProductName.Contains(search)||p.Category.CategoryName.Contains(search));
  if(categoryId>0) q=q.Where(p=>p.CategoryId==categoryId);
  q=price switch {"under30"=>q.Where(p=>p.Price<30000000),"30to50"=>q.Where(p=>p.Price>=30000000&&p.Price<50000000),"50to100"=>q.Where(p=>p.Price>=50000000&&p.Price<100000000),"over100"=>q.Where(p=>p.Price>=100000000),_=>q};
  q=cc switch {"under125"=>q.Where(p=>p.Displacement<125),"125to150"=>q.Where(p=>p.Displacement>=125&&p.Displacement<=150),"over150"=>q.Where(p=>p.Displacement>150),_=>q};
  q=sort switch {"asc"=>q.OrderBy(p=>p.Price).ThenBy(p=>p.ProductId),"desc"=>q.OrderByDescending(p=>p.Price).ThenBy(p=>p.ProductId),_=>q.OrderByDescending(p=>p.CreatedAt).ThenByDescending(p=>p.ProductId)};
  var total=await q.CountAsync(); page=Math.Clamp(page,1,Math.Max(1,(total+8)/9));
  return View(new CatalogVm {Products=await q.Skip((page-1)*9).Take(9).ToListAsync(),Categories=await db.Categories.Where(c=>c.IsActive).OrderBy(c=>c.CategoryName).ToListAsync(),Search=search,CategoryId=categoryId,Price=price,Cc=cc,Sort=sort,Page=page,Total=total});
 }
 public async Task<IActionResult> Details(int id) {
  var p=await db.Products.AsNoTracking().Include(p=>p.Category).Include(p=>p.Images).SingleOrDefaultAsync(p=>p.ProductId==id&&p.Status=="Active"&&p.Category.IsActive);
  return p is null ? NotFound() : View(p);
 }
}
