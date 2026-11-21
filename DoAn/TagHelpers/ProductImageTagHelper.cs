using Microsoft.AspNetCore.Razor.TagHelpers;
namespace Falcon.Web.TagHelpers;
[HtmlTargetElement("img",Attributes="src")]
public class ProductImageTagHelper(IWebHostEnvironment env) : TagHelper {
 public override void Process(TagHelperContext context,TagHelperOutput output) {
  var src=output.Attributes["src"]?.Value?.ToString();
  if(src?.StartsWith("/uploads/products/",StringComparison.Ordinal)!=true) return;
  var name=src["/uploads/products/".Length..];
  if(name!=Path.GetFileName(name) || !File.Exists(Path.Combine(env.WebRootPath,"uploads","products",name))) {
   output.Attributes.SetAttribute("src","/images/missing.svg");
   output.Attributes.SetAttribute("alt","Ảnh sản phẩm chưa được cung cấp");
   output.Attributes.SetAttribute("title","Ảnh sản phẩm chưa được cung cấp");
  }
 }
}
