"use strict";
document.addEventListener("click",async e=>{
 const guest=document.getElementById("loginPrompt");
 const link=e.target.closest("a");
 if(guest&&link){
  let url=new URL(link.href,location.origin);
  if(url.origin===location.origin&&(link.hasAttribute("data-login-required")||["/Booking/Create","/Cart","/Orders","/Booking"].includes(url.pathname))){
   e.preventDefault();const returnUrl=url.pathname==="/Account/Login"?url.searchParams.get("returnUrl"):url.pathname+url.search;
   document.getElementById("promptLogin").href="/Account/Login?returnUrl="+encodeURIComponent(returnUrl||"/");
   document.getElementById("promptRegister").href="/Account/Register?returnUrl="+encodeURIComponent(returnUrl||"/");
   bootstrap.Modal.getOrCreateInstance(guest).show();return;
  }
 }
 const modalLink=e.target.closest("a[data-modal-link]");
 if(modalLink){
  e.preventDefault();
  try{const response=await fetch(modalLink.href,{headers:{"X-Requested-With":"XMLHttpRequest"}});if(!response.ok)throw Error();
   const doc=new DOMParser().parseFromString(await response.text(),"text/html"),content=doc.querySelector("[data-modal-content]");if(!content)throw Error();
   let host=document.getElementById("couponModal");if(!host){host=document.createElement("div");host.id="couponModal";host.className="modal fade";host.tabIndex=-1;host.setAttribute("aria-label","Thông tin mã giảm giá");host.innerHTML='<div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content"><div class="modal-header"><span class="text-warning">FALCON / MÃ GIẢM GIÁ</span><button class="btn-close btn-close-white" type="button" data-bs-dismiss="modal" aria-label="Đóng"></button></div><div class="modal-body"></div></div></div>';document.body.append(host);}
   host.querySelector(".modal-body").replaceChildren(content);if(window.jQuery?.validator?.unobtrusive) window.jQuery.validator.unobtrusive.parse(content);bootstrap.Modal.getOrCreateInstance(host).show();
  }catch{location.href=modalLink.href;}
 }
});
document.addEventListener("submit",e=>{const form=e.target;if(form.matches("[data-confirm]")&&!confirm(form.dataset.confirm)){e.preventDefault();return;}if(form.checkValidity()){form.setAttribute("aria-busy","true");}});
