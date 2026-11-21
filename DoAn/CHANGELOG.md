# CHANGELOG — 20/11/2026

## Nguồn và phạm vi

- Project đầu vào: `FALCON_Website_Integrated(2).zip` (151 mục); đối chiếu byte với bản tích hợp đã phân tích trước: không khác.
- SQL chuẩn: `FALCON_FullDatabase_DongBo_4TaiKhoan(2).sql`; đưa nguyên byte vào Database với tên không có hậu tố `(2)`.
- TV2: `Falcon_Frontend_TV2(1).rar`; không có `.csproj`/backend. Đã dùng bố cục bảng, thống kê, modal Coupons, biểu mẫu Booking và quy tắc coupon/checkout; chuyển thành Razor và C#.
- Prompt: đọc nguyên vẹn TXT trong `FALCON_Prompt_CapNhat_TichHop_TV2_Coupons_Booking.zip`. Mục D còn chữ “placeholder” mâu thuẫn các mục A/I/J; thực hiện yêu cầu cập nhật Coupons CRUD thật.

## Thay đổi

- AccountController, RegisterVm, Login/Register/Profile views: đăng ký Customer + profile transaction; hash Identity V3; returnUrl local; xóa Session khi logout/login; Admin về dashboard.
- Program: server Session, CartService, DataProtection, cấm cache response đã xác thực. Giữ cookie validation theo user/role/status/password và CSRF toàn cục.
- Models/Commerce và FalconDbContext: ánh xạ đầy đủ Coupons, Showrooms, CustomerProfiles, Orders, OrderDetails, TestDriveBookings. Giữ dbo và tên cột SQL, decimal, khóa ngoại, cột computed. Không thêm bảng/migration.
- Cart/Checkout/Orders: tính tiền phía server, idempotency token, khóa transaction, coupon count và rollback; lịch sử riêng theo UserId.
- Booking/Bookings Admin: danh mục động, kiểm tra ngày/giờ/trùng, lưu đầy đủ trường và UserId từ principal, khách chỉ xem/hủy lịch của mình.
- Coupons Admin: thay placeholder bằng Index/Create/Edit/Details/Delete/Toggle; form DTO chống overposting, khóa ghi và fingerprint phát hiện dữ liệu cũ; FK guard, không reset demo.
- Orders Admin: chuyển trạng thái, xác nhận thanh toán thủ công; trừ tồn kho có điều kiện khi hoàn tất, cấm lặp thao tác trừ kho.
- Users TV3: giữ CRUD khách hàng, từ chối tạo/nâng quyền Admin và sửa/xóa/khóa Admin. Ẩn nút không được phép với tài khoản Admin.
- Products TV1: giữ CRUD/ảnh/kiểm tra phụ thuộc, bổ sung kiểm tra tồn kho gốc và optimistic concurrency để không ghi đè số tồn đã thay đổi.
- NhaCungCap TV4: giữ nguyên controller/service/model/views. Layout chung bổ sung menu chức năng mới.
- Public layout, homepage, product detail: loại phần giới thiệu nhóm, menu theo vai trò, CTA thật; modal yêu cầu đăng nhập, fallback không JS. Không tạo ảnh/xe mới.
- CSS/commerce.js: chuyển trải nghiệm modal TV2 sang form POST Razor, không có dữ liệu localStorage. Giao diện vẫn Bootstrap 5 đen–vàng.
- README, báo cáo, checklist SQL, manifest hash, solution và test script cập nhật; chứng cứ cũ chuyển vào docs/history.

## Nghiệp vụ và giới hạn

Chi tiết trong README: cọc dự kiến 10%, đơn chưa thu tiền/giữ xe, kho trừ lúc hoàn tất, hủy coupon đúng một lần, Booking từ ngày mai tới 90 ngày. Không thêm payment gateway, không gửi email/SMS, không có quy trình hoàn tiền tự động. Session in-memory dành cho một instance local.

Build và 173 kiểm tra tự động đã thực hiện; SQL Server/F5/visual browser chưa được xác nhận. Kiểm thử SQLite có thay thế riêng lệnh SQL Server applock, không phải bằng chứng khóa đồng thời SQL Server hoạt động.
