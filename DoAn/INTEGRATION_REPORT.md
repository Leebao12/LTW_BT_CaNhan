# Báo cáo tích hợp FALCON

Ngày thực hiện: 20/11/2026. Báo cáo này thay báo cáo ở `docs/history/INTEGRATION_REPORT_PREVIOUS.md`.

## Kiểm kê và đối chiếu

| Nguồn | Bằng chứng trong file | Xử lý |
|---|---|---|
| Project Integrated(2) | Account chỉ Login/Logout/Denied; Coupons placeholder; nav công khai có Admin; Home có nhóm | Mở rộng đúng các controller hiện có, không tạo controller Coupons trùng |
| SQL mới | 14 bảng chính, FK đầy đủ, 4 demo user; có phụ lục Password | Map đúng SQL, không dùng Password, giữ seed nguyên byte |
| TV2 RAR | HTML/CSS/JS, README Live Server; initialCoupons/localStorage; booking thiếu email/license/notes trong đối tượng JS | Chuyển UI/luật hợp lý thành Razor, lấy toàn bộ dữ liệu nghiệp vụ từ SQL |
| catalog.js TV2 | 10 xe minh họa khác SQL, ảnh hero minh họa | Không nhập vào DB, giữ xe/ảnh của project và SQL chuẩn |
| app.js checkout | Tăng UsedCount trước save order | Thay bằng transaction đơn/chi tiết/coupon, có kiểm tra replay |
| app.js Coupons | Xóa dựa localStorage, reset demo | Kiểm tra Orders.CouponId thật + FK, không có reset |
| Prompt mục D | Một chỗ còn “Coupons placeholder” | Theo mục A/I/J và yêu cầu người dùng mới nhất: full CRUD TV2 |
| Ảnh cần xóa | Section nhóm và 4 card | Xóa section, CSS member-card, shortcut quản trị công khai |

Đã đọc toàn bộ prompt, schema 14 bảng, các phần seed và phụ lục; kiểm tra tất cả tệp gói project, RAR và đối chiếu source baseline bằng hash. Các tệp TV2 bắt buộc đã đọc: coupons.html, booking.html, checkout.html, cart.html, success.html, orders.html, assets/app.js, assets/catalog.js, assets/catalog.css, README. Assets vendor được giữ, không diễn giải thư viện minified là mã nghiệp vụ. `docs/INPUTS_AND_CHANGES.json` lưu inventory/hash và danh sách thay đổi.

## Kiến trúc thực tế sau tích hợp

Một Program, một web csproj net8.0, một solution. FalconDbContext/SqlServer EF Core 8.0.22 quản lý dữ liệu mới; NhaCungCapService/DashboardService giữ ADO.NET từ project cũ và cùng DefaultConnection. Không dùng InMemory/SQLite trong web project; SQLite chỉ xuất hiện ở harness test nằm trong file tài liệu `.cs.txt`, tạo test project tạm khi chạy script.

Controller/DTO phân tách dữ liệu form và entity. `[Authorize(Roles="Admin")]` bảo vệ mọi Admin controller; Customer bảo vệ Cart/Checkout/Orders/Booking. Mọi action ghi là POST với antiforgery toàn cục. Ownership được lọc theo UserId lấy từ claim, không nhận UserId từ form. Response đã đăng nhập no-store. PasswordHash không xuất ra UI. Cột Password không có trong model.

Mapping SQL: CustomerProfiles/UserId unique; Coupon decimals (18,2), dates datetime; Orders/UserId nullable và CouponId nullable; OrderDetails.CarId FK Products, TotalPrice computed, Orders cascade; Booking ngày date, giờ chuỗi 30, license tùy chọn, UserId nullable cho dữ liệu cũ. Sản phẩm giữ column snake_case và giá (15,2). Supplier/import tables vẫn được service TV4 xử lý đúng schema gốc.

Không sửa SQL chuẩn, không thêm migration. SQL mới có phụ lục thêm Password và reset hash demo khi chạy lại; chỉ hướng dẫn dùng trên local và không chạy lại với database đã vận hành. DB cũ nhiều user/đơn/coupon được giữ nguyên. Không thể khẳng định mọi DB cũ có đúng 4 user; chỉ seed mới thiết kế 4 user.

## Đồng thời và dữ liệu

- Serializable + transaction-owned SQL app lock dùng cho checkout/coupon/order state. Tăng coupon count cùng SaveChanges/commit; lỗi detail insert rollback tất cả.
- Token DataProtection chứa UserId/order code/thời điểm; lặp request đã tạo trả về cùng order. Unique OrderCode là hàng rào dữ liệu bổ sung.
- Coupon Edit/Delete/Toggle kiểm fingerprint mới nhất dưới khóa. Chặn xóa khi có Orders ngay cả đơn đã hủy.
- Booking khóa riêng, kiểm tra khoảng chồng giờ giữa cùng người dùng hoặc cùng xe/showroom; giữ seed cũ, không giả định mọi time slot giống TV2.
- Trừ kho một lần khi Completed, SQL conditional update đảm bảo không âm. TV1 form kiểm stock gốc và EF concurrency token khi lưu.
- Không tự ghi/thu tiền. DepositAmount là mức dự kiến, PaymentStatus là trạng thái Admin đối chiếu thủ công. Không có xử lý hoàn tiền trong lần tích hợp.

## Bảo toàn và thay đổi có chủ đích

TV1 giữ CRUD và ảnh, chỉ thêm bảo vệ tồn kho; TV3 giữ CRUD Customer nhưng hạn chế Admin theo yêu cầu mới; TV4 nguyên controller/service/models/views. Các asset từ project đầu vào giữ nguyên byte trừ `wwwroot/css/integrated.css` được mở rộng và bỏ member-card. Thêm commerce.js, không mang app.js/catalog.js localStorage TV2 vào ứng dụng chạy. Không có seed Products trong C#.

Gói bàn giao có một project chạy chính; không đóng kèm RAR TV2/HTML Live Server làm website thứ hai. Ghi công TV2 trong tài liệu. Số lượng coupon, xe, user và lịch hiển thị được truy vấn động; không giả lập 25 coupon trên UI.

## Kết quả kiểm tra

| Phạm vi | Kết quả thực tế |
|---|---|
| .NET 8.0.428 Release build | Thành công, 0 warning / 0 error; Razor compile trong build |
| TestServer + SQLite tạm | 173 kiểm tra qua; chi tiết PASS trong QA_RESULTS.txt |
| Auth/Razor/route | Guest, Customer, Admin; CSRF; chặn nâng quyền; render views mới và CRUD cũ có thể kiểm tra |
| User/Profile | Transaction đăng ký, duplicate, mismatch, RoleId giả; Identity hash |
| Checkout | Server price/ownership, preview no writes, idempotent replay, injected detail failure rollback, retry |
| Coupons | Tạo/sửa/xóa, duplicate, >100%, immutable fields, stale edit, FK reference guard; rule inactive/future/expired/depleted/min/cap |
| Booking | Lưu đầy đủ field, trùng khoảng giờ, đọc/hủy đúng owner, Admin xác nhận |
| Kho/đơn | Chuyển trạng thái, chỉ trừ lúc hoàn tất, chống trừ lặp, stale edit TV1 |
| Bốn seed hash | Xác minh bằng PasswordHasher trực tiếp từ SQL, đều hợp lệ |
| SQL Server thực | CHƯA CHẠY: không có engine/sqlcmd/docker trong môi trường |
| Visual Studio F5 Windows | CHƯA CHẠY |
| Trình duyệt desktop/mobile/modal | CHƯA CHẠY ĐƯỢC: Chromium bị chặn socket, không có ảnh nghiệm thu UI |
| CRUD TV4 SQL, dashboard SQL, Product delete SQL phụ thuộc | Giữ nguồn cũ, cần kiểm tra với SQL Server; không coi render/build là test persistence |

173 là số assertion (gồm token/response), không phải 173 tình huống nghiệp vụ độc lập. Harness bỏ qua `sp_getapplock` trên SQLite; không chứng minh concurrency của SQL Server. Không tuyên bố đã chạy thành công SQL Server/F5 hoặc toàn bộ end-to-end trên Windows.

## Việc cần nghiệm thu trên máy local

Mở solution, cấu hình instance, nhập seed nếu DB mới, chạy checklist SQL đính kèm. Ưu tiên test hai phiên checkout dùng coupon còn 1 lượt; trùng booking đồng thời; kiểm FK thực, constraints/precision và 3 CRUD cũ. Dùng DB thử hoặc bản sao backup để không ảnh hưởng dữ liệu thật. Không có lệnh reset trong ứng dụng.
