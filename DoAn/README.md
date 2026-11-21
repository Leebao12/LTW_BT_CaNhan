# FALCON — ASP.NET Core MVC .NET 8

Bản cập nhật Auth / Cart / Checkout / Booking / Coupons TV2, ngày 20/11/2026.
Một web project, một `DefaultConnection`, database `QuanLyCuaHangXeMay`. Giao diện Bootstrap 5 đen–vàng. Không dùng localStorage để lưu nghiệp vụ.

## Mở bằng Visual Studio và SQL Server local

1. Giải nén ZIP vào thư mục riêng, không chép đè project đang dùng. Mở `FALCON_Website.sln` hoặc `FALCON_Website.csproj` ngay tại thư mục gốc.
2. Dùng Visual Studio 2022 hỗ trợ .NET 8, workload **ASP.NET and web development**, .NET 8 SDK và SQL Server local. SQL Server cần compatibility level 130 trở lên (EF Core 8 sử dụng OPENJSON trong một số truy vấn).
3. Nếu **tạo database mới**, mở `Database/FALCON_FullDatabase_DongBo_4TaiKhoan.sql` bằng SSMS và chạy trên SQL Server local. Đây là bản SQL mới nhất được gửi, giữ nguyên nội dung. Script tự tạo `QuanLyCuaHangXeMay` nếu chưa có.
4. Nếu **database đã tồn tại đúng schema mới**, dùng lại, không chạy lại seed. Sao lưu `.bak` trước khi thay đổi database. Nếu đang có 36 tài khoản từ bản cũ, ứng dụng giữ nguyên; không xóa user hoặc giao dịch để ép số lượng về 4. Phụ lục SQL cập nhật lại hash của 4 tài khoản demo khi chạy lại, nên không chạy lại sau khi đã đổi mật khẩu demo nếu không chủ ý đặt lại chúng.
5. Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json` cho đúng instance đã chọn ở SSMS.

SQL Server default instance, Windows Authentication (cấu hình sẵn):

```json
"DefaultConnection": "Server=localhost;Database=QuanLyCuaHangXeMay;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5"
```

SQL Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyCuaHangXeMay;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5"
```

LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyCuaHangXeMay;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5"
```

Chỉ chọn một cấu hình tương ứng với instance có database của bạn. Nếu dùng SQL Authentication, đặt connection string bằng **Manage User Secrets**, không đưa mật khẩu SQL thật vào mã nguồn. Project không gọi `EnsureCreated`, `Migrate`, không tự chạy SQL và không tự seed.

6. **Build → Build Solution**, chọn profile `FALCON_Website`, bấm **F5**. URL mặc định `http://localhost:5080`. Profile `https` dùng `https://localhost:7080` khi máy đã tin development certificate.
7. Website mở trang công khai. Vào **Đăng nhập** để dùng tài khoản bên dưới. Admin được đưa tới dashboard; Customer ở website khách hàng.

Dùng terminal tại thư mục chứa `.csproj`:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --launch-profile FALCON_Website
```

Không mở file `.cshtml` bằng Live Server. Nếu trang báo chưa tải được dữ liệu, kiểm tra SQL Server đang chạy, đúng instance/database, quyền Windows hiện tại và bảng đã có trong SSMS.

## Tài khoản demo của SQL được cung cấp

| Vai trò | Username | Mật khẩu demo local |
|---|---|---|
| Admin | `falcon.demo.admin` | `admin1@123` |
| Customer | `falcon.demo.kh01` | `quocdung@313` |
| Customer | `falcon.demo.kh02` | `hoanghung@154` |
| Customer | `falcon.demo.kh03` | `gialinh@2k5` |

Đã kiểm tra bốn hash trong phụ lục SQL bằng `PasswordHasher<AppUser>`: đều khớp mật khẩu trên. Đây không phải kết quả đăng nhập vào SQL Server đã triển khai. Database mới theo seed có 1 Admin + 3 Customer; database cũ có thể nhiều hơn và không bị tự làm sạch.

Đăng ký công khai luôn tạo Customer và hồ sơ trong cùng transaction. Sau đăng ký chuyển tới Login. PasswordHash Identity V3 là nguồn xác thực duy nhất. Cột SQL `[Password]` chỉ thuộc dữ liệu demo đã gửi, **không được map vào model, đọc để đăng nhập, trả về giao diện hoặc ghi cho tài khoản mới**. Không dùng mật khẩu demo khi triển khai công khai.

CRUD Users chỉ tạo/sửa vai trò Customer; không được nâng quyền, sửa/xóa/khóa Admin qua màn hình này. Công cụ `dotnet run -- --create-admin` dành cho khôi phục khi database **chưa có bất kỳ Admin nào**, không cần chạy với SQL mẫu và từ chối khi đã có Admin, kể cả Admin đang khóa. Không tự xóa Admin dư trong database cũ.

## Các phân hệ

| Người dùng | Route | Chức năng |
|---|---|---|
| Công khai | `/`, `/Catalog`, `/Catalog/Details/{id}` | Xem xe thật trong SQL, tìm/lọc, xem ảnh đang có |
| Công khai | `/Account/Login`, `/Account/Register` | Đăng nhập, đăng ký Customer |
| Đã đăng nhập | `/Account/Profile` | Đọc hồ sơ của chính mình |
| Customer | `/Cart` | Thêm/sửa/xóa giỏ hàng trong server Session |
| Customer | `/Checkout` | Kiểm tra coupon, tạo Orders + OrderDetails |
| Customer | `/Orders`, `/Orders/Details/{id}` | Xem và hủy đơn của mình theo điều kiện |
| Customer | `/Booking/Create`, `/Booking`, `/Booking/Details/{id}` | Đặt, xem, hủy lịch lái thử của mình |
| Admin | `/Admin` | Dashboard từ dữ liệu SQL |
| Admin | `/Admin/Products` | CRUD sản phẩm TV1, giữ ảnh/kiểm tra ràng buộc |
| Admin | `/Admin/Users` | CRUD khách hàng TV3, bảo vệ tài khoản Admin |
| Admin | `/Admin/NhaCungCap` | CRUD nhà cung cấp TV4 |
| Admin | `/Admin/Coupons` | CRUD Coupons chuyển đổi từ giao diện TV2 |
| Admin | `/Admin/Orders`, `/Admin/Bookings` | Xem và xử lý đơn/lịch hẹn |

Guest chỉ thấy nút **Đăng nhập** trong nhóm thao tác tài khoản ở thanh điều hướng. CTA mua/lái thử mở modal đăng nhập, có liên kết đăng ký. Không bật JavaScript vẫn đến Login qua link/authorization và giữ returnUrl; GET trở lại trang xác nhận, không tự ghi giỏ hàng. Link quản trị chỉ dành cho Admin. Customer nhập URL Admin sẽ bị chuyển tới trang từ chối HTTP 403.

## Quy tắc nghiệp vụ đã chọn

- **Giỏ:** chỉ lưu ProductId/Quantity trong Session phía máy chủ, khóa theo UserId. Giá, tình trạng và tồn kho đọc lại từ Products. Xóa khi logout/login; hết sau 30 phút không hoạt động hoặc khi ứng dụng khởi động lại. Không có bảng giỏ hàng mới. Hai tab cập nhật giỏ cùng lúc có thể lấy lần ghi sau cùng, nên nên thao tác tuần tự; checkout vẫn kiểm tra lại dữ liệu.
- **Đơn:** nhận yêu cầu đặt mua, chưa giữ xe, chưa thu tiền. Giá và tồn kho kiểm tra trong transaction. Mã gửi đơn được bảo vệ bằng Data Protection, hạn 2 giờ; gửi lại cùng mã chỉ trả về đơn đã tạo. `PaymentStatus=Pending`, `OrderStatus=Processing`.
- **Coupon:** giá trị giảm bị chặn trong khoảng 0..SubTotal; giảm phần trăm có trần nếu khai báo, kể cả trần 0. Cọc dự kiến = 10% tổng sau giảm. Preview không ghi dữ liệu. Tăng UsedCount cùng transaction lưu đơn/chi tiết; lỗi thì rollback toàn bộ. Hủy đơn đủ điều kiện hoàn lại lượt đúng một lần; vẫn giữ liên kết coupon trong lịch sử đơn hủy và chặn xóa mã đã có bất kỳ đơn tham chiếu.
- **Đồng thời:** các ghi đơn/coupon dùng Serializable + `sp_getapplock` transaction-owned `FALCON:Commerce`. Booking dùng khóa riêng `FALCON:Booking`. Khóa áp dụng cho các luồng ghi trong ứng dụng, không kiểm soát thao tác SQL thủ công ngoài ứng dụng. SQL login cần quyền chạy `sp_getapplock` và thao tác các bảng. Đây là lựa chọn khóa rộng phù hợp demo nhỏ; chưa kiểm thử tải nhiều người.
- **Sửa coupon:** Code/DiscountType/DiscountValue giữ nguyên; UsedCount không nhận từ request. Version là fingerprint của trạng thái DB để phát hiện form cũ; UsageLimit không thấp hơn UsedCount. Có lọc và phân trang, trạng thái tính từ DB/giờ hiện tại. Không có reset dữ liệu mẫu.
- **Tồn kho:** chỉ trừ một lần khi Admin chuyển Delivering → Completed và đã xác nhận Paid. Không trừ lúc tạo đơn. TV1 sửa sản phẩm kiểm tra tồn kho gốc và concurrency token để không ghi đè số tồn vừa thay đổi bởi đơn hàng. Những đơn mẫu Completed trong SQL được giữ nguyên, không trừ kho lại.
- **Hủy đơn:** Customer tự hủy khi Processing/Pending. Admin hủy đơn chưa hoàn tất và chưa thu tiền (Pending/Failed). Đơn Deposited/Paid không có quy trình hoàn tiền tự động; cần xử lý ngoài hệ thống. PaymentStatus chỉ cập nhật thủ công sau khi đối chiếu tiền thực nhận, không tích hợp cổng thanh toán/QR thu tiền tự động.
- **Booking:** chọn xe đang bán và showroom hoạt động từ SQL; ngày từ ngày mai tới 90 ngày tới. Giữ 3 khung giờ TV2. Một khách không nhận các lịch chồng giờ; cùng xe + showroom chỉ nhận một lịch chồng giờ ở trạng thái Pending/Confirmed. So sánh khoảng giờ nên nhận biết cả các khung giờ cũ trong seed. Lịch Completed/Cancelled/NoShow không chặn chỗ. Khách hủy trước giờ hẹn hơn 2 tiếng. Admin chỉ xác nhận lịch tương lai; hoàn tất/vắng mặt sau giờ bắt đầu. Bằng lái tùy chọn, không lưu trong trình duyệt hay log.
- **Thời gian:** nghiệp vụ dùng giờ Việt Nam UTC+7, lưu datetime local phù hợp SQL. Khi seed bằng GETDATE(), SQL Server local nên dùng múi giờ Việt Nam.

## Đóng góp và bảo toàn nguồn

TV1: Products; TV3: Users; TV4: NhaCungCap. **TV2 Lê Huy Quốc Bảo cung cấp frontend Coupons/Booking** trong `Falcon_Frontend_TV2(1).rar`: bố cục, bảng, thống kê, modal, form và logic mô phỏng. Backend C#/EF/SQL, authentication, transaction, phân quyền và kiểm soát đồng thời được viết trong lần tích hợp này; không ghi nhận TV2 đã có backend sẵn.

Giữ toàn bộ ảnh/tài sản giao diện có sẵn và 6 mẫu xe trong SQL chuẩn; không nhập 10 xe minh họa trong catalog.js TV2, không tạo ảnh/xe mới. Section giới thiệu thành viên và hậu tố TV trên menu đã xóa. Chỉ lưu phân công trong tài liệu.

## Kiểm thử và giới hạn đã xác nhận

- `dotnet restore` / Release build: thành công, 0 warning, 0 error (xem `docs/verification/BUILD.txt`).
- 173 kiểm tra tự động đã qua trên ASP.NET TestServer + SQLite tạm trong bộ nhớ: Razor, auth/CSRF, CRUD Coupons, Session, đăng ký/hashing, ownership, checkout idempotency, rollback do lỗi chèn chi tiết, booking, chuyển trạng thái và tồn kho. Các kiểm tra bao gồm kiểm tra token/response, không phải 173 kịch bản nghiệp vụ độc lập.
- **Chưa chạy SQL Server thật**, chưa chạy script seed trong SSMS, chưa xác nhận `sp_getapplock` và race nhiều kết nối SQL Server. Test SQLite bỏ qua riêng lệnh applock. Nó không thay thế kiểm thử SQL Server.
- Chromium không khởi động được vì môi trường chặn socket (`Operation not permitted`). Đã compile/render Razor; **chưa xác nhận hiển thị bằng trình duyệt** trên desktop/mobile và modal tương tác.
- Chưa mở Visual Studio/F5 trên Windows trong môi trường này. Có solution, launch profile, SQL chuẩn và hướng dẫn để chạy local; không coi đó là kết quả đã nghiệm thu trên máy bạn.

Chạy lại kiểm thử tách biệt bằng PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Test-Isolated.ps1
```

Script tạo test project trong thư mục tạm và SQLite trong bộ nhớ, không thay database cấu hình, không thêm xe vào SQL của bạn. Xem `docs/SQL_ACCEPTANCE_CHECKLIST.md` và chạy `Database/Verify_ReadOnly.sql` để nghiệm thu SQL local. `docs/history/` chỉ là bằng chứng bản tích hợp trước; kết quả hiện tại ở `docs/verification/`.
