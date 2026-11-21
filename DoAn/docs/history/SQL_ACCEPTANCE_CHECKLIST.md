# Checklist nghiệm thu với SQL Server thật

Chạy trên database thử có đúng schema. Không dùng database thật để thử thao tác xóa. Ghi ngày, người chạy, dữ liệu đầu vào và kết quả thực tế; các ô dưới đây chưa được đánh dấu vì phiên tích hợp không có SQL Server.

## Cài đặt và phân quyền

- [ ] Import script chuẩn trên DB mới; 14 bảng và seed đúng. Không chạy lại để reset.
- [ ] DefaultConnection trỏ QuanLyCuaHangXeMay; cả 3 CRUD đọc cùng DB.
- [ ] Tạo Admin bằng CLI, hash khác mật khẩu; không có tài khoản mặc định.
- [ ] Đăng nhập đúng/sai/LOCKED/INACTIVE; Customer bị từ chối Admin; request thiếu token bị 400.
- [ ] Đổi role, status, password bằng Admin khác làm cookie cũ mất quyền ở request kế tiếp.

## TV1 Products

- [ ] Danh sách, ảnh chính, Details; tìm tên/hãng/slug, filter, phân trang.
- [ ] Tạo xe hợp lệ; xem trong SSMS và trên Catalog cùng giá/tồn.
- [ ] Slug trùng, giá/tồn âm, cc=0, status sai, hãng không tồn tại bị từ chối.
- [ ] Sửa giá/tồn/nổi bật/status; không upload thì ảnh cũ còn nguyên.
- [ ] Upload JPEG/PNG/WEBP hợp lệ; kiểm tra file GUID và ProductImages.
- [ ] Từ chối file đổi đuôi giả, >5 MiB; ảnh mới được dọn khi SaveChanges lỗi.
- [ ] Xóa xe thử không giao dịch; ProductImages cascade, xe khác không đổi.
- [ ] Với mỗi FK OrderDetails.CarId, TestDriveBookings.CarId, ChiTietDonNhap.product_id: tạo dữ liệu thử hợp lệ rồi xác nhận xóa xe bị chặn. Kiểm tra race với khóa ngoại SQL.

## TV3 Users

- [ ] Tạo, tìm, chi tiết; Username/Email trùng bị chặn bởi app và unique SQL.
- [ ] Sửa Email/Role/Status; bỏ mật khẩu mới không đổi hash; nhập mới có xác nhận thì hash đổi và đăng nhập bằng mật khẩu mới.
- [ ] Xóa user không liên kết; mỗi trường hợp có Profile/Order/Booking chuyển LOCKED và giữ lịch sử.
- [ ] Kiểm tra UserId bigint, transaction và cạnh tranh ghi; lỗi không hiển thị thành công giả.

## TV4 NhaCungCap

- [ ] Danh sách 2 NCC seed có phone/email/address NULL không lỗi mapper.
- [ ] Create/Edit/Details/tìm kiếm; phone rỗng lưu NULL được.
- [ ] Giới hạn tên100/phone15/email50/address255; loại chỉ Xe/Phu tung/Ca hai.
- [ ] POST Delete có bước xác nhận; không đơn nhập thì xóa, có DonNhapKho thì active=0, không mất đơn.
- [ ] Kiểm tra transaction và trường hợp bản ghi vừa bị xóa ở phiên khác.

## Toàn website

- [ ] 4 menu CRUD/trang chờ đúng route, logo và nút về website/đăng xuất hoạt động.
- [ ] Coupons chỉ có thông báo chờ, không Create/Edit/Delete/UsedCount.
- [ ] Dashboard khớp truy vấn SQL; completed orders mới tính bán/revenue; không cộng tiền cọc hai lần.
- [ ] Kiểm tra tháng UpdatedAt và năm; Completed thiếu UpdatedAt không được gán tháng giả.
- [ ] 6 ảnh chính đủ, ảnh phụ thiếu có placeholder rõ; giao diện 390px/768px/1440px không tràn trang.
- [ ] Website không dùng localStorage làm nguồn giá/tồn/người dùng/đơn hàng.
- [ ] Chạy dotnet watch trên Windows, thử sửa View và tải lại; kiểm tra console/network không có lỗi ngoài dự kiến.
