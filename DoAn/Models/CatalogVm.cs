using Falcon.Web.Models.Admin;
namespace Falcon.Web.Models;
public class CatalogVm {
 public List<Product> Products {get;set;}=new(); public List<Category> Categories {get;set;}=new();
 public string? Search {get;set;} public int? CategoryId {get;set;} public string? Price {get;set;} public string? Cc {get;set;} public string? Sort {get;set;}
 public int Page {get;set;} public int Total {get;set;} public int Pages => Math.Max(1,(Total+8)/9);
}
