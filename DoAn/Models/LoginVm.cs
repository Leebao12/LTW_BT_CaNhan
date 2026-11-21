using System.ComponentModel.DataAnnotations;
namespace Falcon.Web.Models;
public class LoginVm {
 [Required, StringLength(50), Display(Name="Tên đăng nhập")] public string Username {get;set;}="";
 [Required, StringLength(100), DataType(DataType.Password), Display(Name="Mật khẩu")] public string Password {get;set;}="";
 public string? ReturnUrl {get;set;}
}
