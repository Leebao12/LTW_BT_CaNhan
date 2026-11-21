# Nghiệm thu SQL Server local — chưa thực thi trong môi trường tích hợp

## Chuẩn bị

- Sao lưu database đang dùng bằng SSMS → Tasks → Back Up. Không DROP, không DELETE tài khoản cũ.
- Nếu kiểm tra seed mới: dùng SQL Server instance thử chưa có QuanLyCuaHangXeMay; chạy SQL chuẩn một lần. Không chạy trên database sản xuất.
- Mở solution, chọn đúng DefaultConnection/instance và chạy F5. Chạy Verify_ReadOnly.sql trong SSMS. Kỳ vọng mới: 14 bảng chính (+ cột Password demo, không phải bảng mới), 1 Admin + 3 Customer, 6 Products, 25 Coupons, 12 Orders, 10 Bookings theo seed. Nếu DB có dữ liệu cũ/tạo thêm thì số đếm khác hợp lệ.

## Guest / Auth / Roles

1. Mở cửa sổ riêng tư, vào Home/Catalog/Details: dữ liệu SQL; không có mục Quản trị và section nhóm. Navbar tài khoản chỉ Đăng nhập.
2. CTA mua/thêm giỏ/lái thử mở modal đen–vàng, đóng được bằng Esc/nút x. Login/Register giữ xe. Tắt JS thử fallback; GET không tạo giỏ/đơn/lịch.
3. Đăng nhập 4 demo account. Admin về dashboard, Customer ở public. Customer /Admin/Coupons → trang từ chối HTTP 403; Guest /Admin → Login.
4. Đăng ký Customer mới, xem Users.RoleId/Status và CustomerProfiles đúng UserId; PasswordHash băm, Password NULL. Thử trùng username/email, mật khẩu xác nhận sai, RoleId=Admin giả; không có user mới khi thất bại.
5. Admin thử tạo/nâng quyền Admin hoặc khóa/xóa Admin bằng POST: bị chặn; các thao tác Customer của TV3 vẫn dùng được. Đăng xuất POST cần CSRF; giỏ bị xóa.

## Cart / Orders / Coupons

6. Với Customer mới: thêm xe còn hàng, đổi số lượng, xóa; thử 0, số âm, vượt tồn kho, xe ngừng bán. Kiểm tra giá từ Products, không tin số tiền gửi client.
7. Admin tạo mã QA có thời hạn hiện tại, giới hạn 1, mức 10%/trần phù hợp. Thử mã trùng chữ hoa/thường, phần trăm >100, giá trị <=0, min/max âm, ngày sai, limit < used.
8. Thử preview mã hợp lệ: chưa có Orders mới, UsedCount không đổi. Mã tắt/hết hạn/chưa bắt đầu/hết lượt/đơn dưới min phải báo lỗi; total không âm, cọc 10% sau giảm.
9. Gửi đặt mua: Orders.UserId đúng; có OrderDetails, CarId hợp lệ, tên/giá snapshot; coupon FK đúng, count +1, trạng thái Processing/Pending. Refresh/resubmit cùng form không tạo đơn thứ hai.
10. Dùng hai trình duyệt Customer khác nhau cùng checkout coupon còn 1 lượt: chỉ một đơn được dùng mã; UsedCount không vượt UsageLimit. Đây là bài kiểm tra khóa SQL Server bắt buộc chưa được chứng thực trên SQLite.
11. Hủy Processing/Pending: count -1; hủy lại không giảm tiếp. Mã vẫn có Orders tham chiếu nên không được xóa; chuyển IsActive=false được. Tạo mã hoàn toàn mới chưa dùng rồi xóa có xác nhận, kiểm tra DB.
12. Hai tab sửa coupon: lưu tab A, tab B phải báo stale. Code/type/value/used không được thay qua POST giả. Bật/tắt nhanh cũng kiểm version.
13. Admin đưa đơn Processing → Confirmed → Delivering → Completed với Paid: trừ kho một lần, cập nhật doanh thu. Thử gửi lại cùng POST; không trừ nữa. Thử thiếu tồn kho và Completed chưa Paid: không cập nhật kho/đơn.
14. Mở form Products trước khi hoàn tất đơn, sau đó lưu form cũ: phải báo tồn kho thay đổi. Tải lại rồi sửa hợp lệ. Đơn Paid/Deposited không được tự hủy ở luồng này; không có hoàn tiền tự động.

## Booking

15. Tạo lịch từ ngày mai, chọn xe/showroom từ DB, điền đủ email/notes/license tùy chọn. Kiểm SQL UserId, CarId, ShowroomId, BookingCode, Pending, contact fields đúng.
16. Thử ngày quá khứ/hôm nay/>90 ngày, showroom ngừng hoạt động, xe Discontinued, ID giả, slot ngoài danh sách: bị chặn.
17. Hai phiên đặt cùng xe + showroom + khoảng giờ, hoặc cùng khách đặt giờ chồng ở showroom khác: chỉ một lịch hoạt động được nhận. Kiểm cả slot cũ 10:00–11:00 với slot mới 09:00–10:30.
18. Đăng nhập Customer khác: không xem/hủy order/booking theo ID của người khác; 404. Lịch seed UserId NULL chỉ Admin nhìn thấy, không ghép theo phone/email.
19. Admin xác nhận lịch tương lai, khách hủy trước giờ hẹn hơn 2 tiếng; trạng thái lưu đúng. Không nhận lại lịch trùng do form gửi lặp. Admin hoàn tất/vắng mặt chỉ sau giờ bắt đầu.

## Ba CRUD và UI

20. Products: tạo dữ liệu thử do người kiểm thử chủ động, sửa giá/tồn/ảnh, tìm/lọc, giữ ảnh khi không upload; xóa chặn order/booking/import FK và cascade ProductImages khi được phép. Không xóa xe gốc để thử.
21. Users: Create/Edit/Details/Delete Customer; user có giao dịch/hồ sơ chuyển LOCKED, không mất lịch sử. Không lộ hash/password trên UI/HTML.
22. NhaCungCap: thêm/sửa/xem/tìm/lọc/xóa theo FK phiếu nhập; dashboard thống kê từ cùng DB.
23. Desktop và mobile 390px: kiểm menu, cuộn bảng, form lỗi, modal Coupon Create/Edit/Details/Delete, bàn phím/focus, ảnh gốc, link MVC. Không còn .html/Live Server/localStorage.
24. Xem log/DevTools: không có giấy phép lái xe, password hoặc SQL parameter nhạy cảm trong log; không EnableSensitiveDataLogging.

Ghi kết quả thực tế, ngày/instance và ảnh minh chứng trước khi nghiệm thu. Không đánh dấu PASS các mục chưa chạy.
