# Báo cáo hợp nhất FALCON

Ngày bàn giao: 20/11/2026. Phạm vi: hợp nhất mã nguồn theo đủ ba prompt; không triển khai CRUD TV2, không thay SQL hoặc reset dữ liệu.

## 1. Nguồn đã đối chiếu và quyết định

Tên hậu tố trong prompt khác file thực tế; nhận diện bằng nội dung. Đã giải nén 5 archive và đọc SQL trực tiếp (SQL không phải archive). Ba TXT được đọc theo thứ tự 01 → 02 → 03. `docs/SOURCE_INVENTORY.json` ghi cây file archive; `docs/INPUT_HASHES.json` ghi SHA-256 đủ 9 đầu vào.

| Đầu vào thực tế | File/lớp quan trọng | Bảng / route | Phụ thuộc & xung đột | Quyết định |
|---|---|---|---|---|
| TV1_CRUD_Products_DaSua_HoanChinh(4).zip | Program, .csproj, Data/MotorcycleDbContext; ProductController.cs chứa class ProductsController; Areas/Admin/Models; Products Views; DashboardService | Products, Categories, ProductImages; /Admin/Products | Namespace TV1; root chuyển tới Products; thiếu DI dashboard; layout chỉ TV1 | Làm nền; giữ CRUD, validate/upload/FK; đổi tên file ProductsController, gộp context; thêm trang khách hàng/dashboard |
| Falcon_TV3(3).rar | UsersController, Models/AppUser/UserViewModels, Data/FalconDbContext, Users Views | Users, Roles, refs CustomerProfiles/Orders/TestDriveBookings; /Admin/Users | Context riêng; _AdminLayout riêng; Username nonnullable trên Edit không được post; chưa có auth bảo vệ | Giữ controller/views/hash; gộp mapping; sửa model hiển thị, thêm phân trang, transaction xóa/khóa và auth chung |
| Thành viên 4(3).rar | NhaCungCapController/Service/Model, 4 Views; QuanLyKho.sql | NhaCungCap, DonNhapKho; /Admin/NhaCungCap | Chuỗi schema SQL cũ, độ dài sai; phone GetString khi NULL; loại nhập tự do | Giữ ADO tham số và Serializable; sửa schema lengths/types/DBNull, dropdown loại; logger khi SQL lỗi |
| Falcon_Frontend(4).rar | index/catalog/detail, 11 trang khác, assets/catalog.css/bootstrap/hero, app.js/catalog.js | Frontend tĩnh, localStorage | Giá/tồn demo khác SQL; HTML link và dữ liệu mock | Tái sử dụng banner, CSS, Bootstrap; chuyển 3 trang sang Razor SQL; admin tĩnh thay bởi CRUD; nghiệp vụ thiếu backend chuyển trang chờ |
| Database_QuanLyXeMay_ThongNhat(5).sql | 14 CREATE TABLE, index, constraints, seed | QuanLyCuaHangXeMay | Khác SQL riêng TV4 và SRS cũ | Nguồn schema cao nhất; giữ nguyên byte trong Database, không thực thi thay đổi schema |
| HoSoDacTaChiTiet_QLyXeMay(3).zip | HoSoDacTaChiTiet_QLyXeMay.docx, 4 phân hệ | Catalog, TV2, Users/Auth, kho/admin | PHP/PDO/MySQL ở TV1, ô tô/B1-B2 ở TV2, QuanLyKhoXe/HoaDonBan ở TV4 | Dùng yêu cầu nghiệp vụ phù hợp; SQL Server/.NET8 và phân công mới có ưu tiên cao hơn |

## 2. Các xung đột và cách sửa

1. **Ba startup/project:** lấy TV1 làm nền, chỉ một `FALCON_Website.csproj`, `Program.cs`, `DefaultConnection`; namespace `Falcon.Web` ở C#/Razor/imports. Không đưa .csproj, Program, appsettings riêng TV3/TV4 vào runtime.
2. **Hai DbContext:** `Data/FalconDbContext.cs` gộp Products/Categories/ProductImages của TV1 và Users/Roles/các reference entities của TV3. Snake_case được mapping tường minh. UserId và FK nullable giữ long/long?. Các reference entities chỉ phục vụ đọc quan hệ, không dùng để tạo Orders/Profiles/Bookings. Không có migrations/EnsureCreated.
3. **ProductController tên file khác class:** đổi tên file; không tạo class Products thứ hai. Bổ sung catch FK khi ghi để trả form và dọn ảnh vừa upload nếu quan hệ thay đổi giữa kiểm tra và SaveChanges.
4. **SQL TV4:** SQL riêng có TenNhaCungCap150, phone20, email150, địa chỉ250 và loại có dấu; SQL chung yêu cầu 100/15/50/255 và `Xe|Phu tung|Ca hai`. Sửa model/SqlParameter/Views theo SQL chung. Phone nullable ở form, mapper và parameter. Email/phone dùng VarChar đúng schema; email kiểm tra ASCII tránh mất ký tự với varchar.
5. **Users Edit:** Username chỉ để hiển thị, không được bind hoặc bắt buộc từ request; nạp lại từ DB. Bỏ trống mật khẩu mới thì giữ hash. Thêm transaction Serializable quanh kiểm tra/xóa hoặc LOCKED; lỗi ghi có log và thông báo. Chặn tự xóa/khóa/hạ quyền tài khoản đang dùng.
6. **Layout:** gộp menu thành `_LayoutAdmin` có 5 mục, nhận active controller; hai layout Admin/khách hàng hỗ trợ Styles/Scripts. Giữ nội dung forms/views của từng thành viên. Thay nút icon-only bằng chữ với các hành động chính; không cần CDN icon. Có CSS responsive cho sidebar/menu và table-responsive.
7. **Không có auth trong nguồn CRUD:** thêm cookie đăng nhập Users/Roles, PasswordHasher, kiểm tra ACTIVE và role tại server. Cookie mất hiệu lực khi trạng thái/role/hash đổi. Có CSRF toàn cục, local ReturnUrl, giới hạn POST Login. Không tạo mật khẩu mặc định; lệnh terminal tạo Admin đầu tiên.
8. **Frontend giả:** bỏ app.js/catalog.js mô phỏng khỏi runtime; không load localStorage. Giá/tồn/catalog từ EF chung; trạng thái Active và hãng active kiểm tra cả danh sách lẫn chi tiết. Hết hàng hiển thị rõ, không nút tạo giao dịch giả.
9. **Dashboard:** giữ và mở rộng service TV1; query Products/Users/NhaCungCap/Orders/OrderDetails, không query KhoXe/HoaDonBan. Bổ sung KPI hết hàng/nổi bật/tài khoản/NCC. Biểu đồ thanh HTML/CSS lấy số thực, có số tiền đọc được; không thêm Chart.js vì prompt cho phép biểu đồ phù hợp và không cần tải thư viện ngoài. `SoXeConTrongKho` được giữ trong service gốc nhưng không hiển thị KPI trùng với tổng tồn.
10. **Ảnh:** giữ nguyên 8 file xe nguồn; 6 ảnh chính SQL có file đúng. Thiếu `sh125-side.jpg`, `sh125-engine.jpg`, `winnerx-detail.jpg`. TagHelper thay src thiếu bằng placeholder có nhãn, không giả tạo góc ảnh/không sửa SQL. Gallery lấy src đã được kiểm tra. Ảnh vật lý cũ không tự xóa khi sửa/xóa xe; có thể cần dọn file mồ côi bằng quy trình riêng sau này.

## 3. Dấu vết đóng góp và thay đổi file

- **TV1 / Hoàng Ngọc Gia Bảo:** giữ ProductsController và sáu Views Products, models Category/Product/ProductImage/ProductViewModels; giữ mapping và DashboardService làm nền. Model được chuyển từ Areas/Admin/Models sang Models/Admin và ViewModels/Admin, namespace cũ được thống nhất.
- **TV3 / Lê Quang Bảo:** giữ UsersController, AppUser/AppRole/reference entities, UserViewModels và năm Views Users; thay layout, mapping, validation Edit, pagination và transaction; password hasher không thay bằng cơ chế mới không tương thích.
- **TV4 / Hoàng Hải Anh:** giữ NhaCungCapController, service ADO, model và bốn Views; sửa các lỗi schema và NULL, bảo toàn quyết định xóa/ngừng hoạt động trong transaction.
- **TV2 / Lê Huy Quốc Bảo:** chỉ tạo CouponsController.Index và View chờ, không tạo Model/Service/POST CRUD. Chưa đụng UsedCount.
- **File mới:** Program thống nhất, LoginVm, AccountController, AdminBootstrap, HomeController khách hàng, CatalogController/CatalogVm, Home Admin controller, Coupons placeholder, Razor Catalog/Home/Login/Pending/Error, ProductImageTagHelper, integrated.css/js, README/report/checklist.
- **Loại khỏi bản chạy:** bin/obj/.vs và template Home/Privacy của nguồn; Program/csproj/config trùng; layouts riêng; QuanLyKho.sql cũ; HTML/JS mô phỏng; không chạy script SQL phân tán. Nguồn upload ban đầu không bị sửa/xóa. Inventory/hash cho phép đối chiếu với file nhóm đã nộp.
- `docs/FILE_PROVENANCE.json` ghi nguồn tương ứng và trạng thái file tích hợp; `docs/FINAL_FILES.sha256` ghi hash các file bàn giao (trừ chính manifest).

## 4. Mức hoàn thành thực tế

| Phân hệ | Đã tích hợp | Bằng chứng hiện có | Chưa xác minh |
|---|---|---|---|
| Products TV1 | CRUD, ảnh, lọc/phân trang, xác nhận xóa, kiểm tra 3 quan hệ | Build C#/Razor; render; InMemory tạo/sửa/slug trùng/giữ ảnh/validation | SQL unique/FK/cascade thật, upload lưu file và rollback, xóa với giao dịch thật |
| Users TV3 | CRUD, Role, 3 trạng thái, hash, khóa khi liên kết, phân trang | InMemory tạo/trùng/sửa/giữ hash; login/CSRF/revocation | Transaction SQL xóa/khóa, cạnh tranh dữ liệu, SQL collation unique |
| NhaCungCap TV4 | CRUD ADO và chuyển ngừng hoạt động, NULL/length/type đúng SQL | Build; render Views bằng fixture; DataAnnotations | SqlDataReader với NULL thật, ExecuteNonQuery, transaction/FK thật |
| Dashboard | KPI và doanh thu từ query SQL thống nhất | Build và render với fixture 12 tháng bằng 0 | Truy vấn/đối soát với Orders SQL thật |
| Catalog/chi tiết/trang chủ | Razor đọc EF chung | Render và tìm/lọc trên InMemory | SQL query/data/hiệu năng thực |
| Auth | Cookie và Admin role/ACTIVE phía server | Anonymous redirect; login/hash/revoke và CSRF trong harness | Kết nối xác thực SQL thật; hosting HTTPS/proxy/session persistence |
| Coupons | Trang chờ | Razor render, câu thông báo chính xác; URL Admin được bảo vệ | CRUD dành riêng cho TV2 |

## 5. Kiểm thử đã thực thi

Môi trường kiểm thử: Debian, .NET SDK 8.0.428, ASP.NET Core runtime 8.0.34. NuGet EF Core 8.0.22. **Build không được coi là bằng chứng SQL hoạt động.**

- `dotnet --info`: đã chạy, SDK đúng .NET 8.
- `dotnet restore`: thành công.
- `dotnet build --no-restore`: thành công; kết quả cuối trong `docs/verification/build-final.log`.
- `dotnet run --no-build --launch-profile FALCON_Website`: Kestrel nghe localhost:5080.
- `dotnet watch run` hot reload ban đầu bị vòng lặp build trong môi trường sandbox. Đã thử lại với PATH chứa SDK, polling watcher và `dotnet watch --no-hot-reload run --launch-profile https`: nghe 7080/5080, GET Login HTTP 200. Dừng tiến trình watch bằng SIGTERM không thoát trong hạn chờ của harness; không phải kiểm chứng hot reload hoàn chỉnh. Xem `watch-final.log`.
- HTTP trực tiếp trên app dùng cấu hình SQL mặc định: Login/Pending/7 tài nguyên kiểm tra trả 200; 5 route Admin trả 302 tới Login; POST Logout thiếu token trả 400.
- `/`, `/Catalog`, `/Catalog/Details/1` trả **503** do không có SQL Server tại localhost. Log SqlClient xác nhận không kết nối được. **Không chạy script khởi tạo, không ghi vào SQL, không tuyên bố CRUD SQL đã đạt.**
- Harness riêng dùng `WebApplicationFactory` + EF InMemory; không thêm provider này vào web csproj. Kiểm tra controller/Razor/auth/validation và fixture render dashboard/NCC. **57 kiểm tra đạt** trong harness (bao gồm kiểm tra token và render; không phải 57 ca CRUD SQL). Kết quả cuối trong `isolated-qa.log`, có mã harness và script để tái lập. InMemory không mô phỏng SQL CHECK, unique constraint, FK, transaction hoặc kiểu/collation SQL Server.
- Thử khởi chạy Chromium để kiểm tra viewport bị sandbox từ chối socket của process singleton. **Chưa kiểm thử giao diện bằng trình duyệt thực hoặc xác nhận bố cục ở mọi kích thước.** Razor được render/compile, CSS responsive đã rà soát mã; không đánh dấu visual QA đạt.

## 6. Phần chưa có backend và giới hạn

Không triển khai giỏ hàng, checkout/cọc/QR/thanh toán, tra cứu đơn, hoàn tất bán/trừ tồn, xử lý đơn nhập/cộng tồn, đăng ký lịch lái thử, đăng ký tài khoản công khai, hồ sơ cá nhân, đổi mật khẩu tự phục vụ và lịch sử mua xe. Trang chờ không nhận dữ liệu, không hiện “đã đặt thành công”. Users CRUD và login thật không đồng nghĩa đã hoàn thành toàn bộ SRS TV3.

SRS cũ đề xuất KhoXe, HoaDonBan, MySQL/PDO và có nội dung ô tô không được áp vào runtime. Không tạo bảng/vw_ cũ để né mapping. Ba prompt và SQL mới nhất được ưu tiên. MSSV TV4 trống trong SRS, không tự điền.

Thiết kế hiện tại phù hợp đồ án nhưng chưa chứng nhận production: chưa có optimistic concurrency cho sửa đồng thời Products/Users, lần lưu sau có thể ghi đè lần trước; chưa có kiểm thử tải, audit log nghiệp vụ, reset password, MFA hay kiểm thử bảo mật toàn diện. Host production cần HTTPS, quyền ghi upload, cấu hình DataProtection key bền vững/bảo vệ key và logging phù hợp. Không tự deploy hosting trong nhiệm vụ này.

## 7. Việc cần làm trên Windows trước nghiệm thu

Thực hiện README mục 2–6, tạo database thử và Admin do nhóm tự chọn mật khẩu. Chạy checklist SQL; đối soát dữ liệu trực tiếp trong SSMS sau từng thao tác. Dữ liệu mẫu kỳ vọng khi vừa khởi tạo: **6 mẫu xe, 65 xe tồn, 1 mẫu hết hàng, 4 nổi bật, 2 NCC hoạt động, 0 xe bán, doanh thu 0**. Users ban đầu 0, sau bootstrap Admin là 1. Đây là số tính từ seed SQL, **không phải kết quả query đã chạy**.

Chỉ sau khi chạy checklist SQL và kiểm tra trình duyệt trên máy nhóm mới kết luận sẵn sàng nộp/demo đầy đủ.
