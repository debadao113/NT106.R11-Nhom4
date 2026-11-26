# HotelDesk — NT106, nhóm 4

Ứng dụng WinForms quản lý phòng, nhận/trả phòng, khách hàng và hồ sơ nhân viên.

## Công nghệ và luồng dữ liệu

- C# / .NET Framework 4.7.2, Windows Forms, Guna UI2.
- Supabase Auth: đăng nhập bằng email hoặc tên tài khoản nhân viên, token chỉ giữ trong bộ nhớ; tự làm mới phiên khi cần.
- Supabase PostgreSQL: database chung cho cả nhóm.
- HTTPS REST API bằng HttpClient; phân quyền bằng RLS và các hàm PostgreSQL.

```text
WinForms → HTTPS → Supabase Auth / Data API → PostgreSQL
```

Chỉ còn **một project WinForms**. Không cần cài SQL Server, chạy server TCP hay máy ảo.

## Chạy dự án

1. Cài Visual Studio 2022+ với workload .NET desktop development và .NET Framework 4.7.2 targeting pack.
2. Làm theo [thiết lập Supabase](docs/SUPABASE_SETUP.md) để tạo database, tài khoản và cấu hình URL/public key.
3. Mở PowerShell trong thư mục dự án, chạy:

```powershell
.\scripts\Setup-Supabase.ps1 -ProjectUrl "https://PROJECT.supabase.co" -PublishableKey "sb_publishable_KEY_THAT"
.\scripts\Build-Team.ps1 -Configuration Debug
```

Mở `ChuuongTrinhQuanLyKhachSan.sln`, chạy project cùng tên, đăng nhập bằng email đã được cấp quyền. Không có mật khẩu mặc định. Cách tạo tài khoản tên `nhom4` và `Long` có trong hướng dẫn Supabase. Nếu mở exe đã build, gửi đủ các file trong thư mục output, gồm DLL và `.exe.config`.

`App.config` chứa URL và publishable key công khai của project nhóm; `App.local.config` là cấu hình dùng khi build trên máy và được Git bỏ qua. Không đưa secret key, service_role key hay mật khẩu database vào app.

## Cấu trúc mã nguồn

| Thư mục | Nội dung |
| --- | --- |
| `Views/` | Cửa sổ đăng nhập, dashboard, phiên đăng nhập |
| `Views/Controls/` | Các màn hình nghiệp vụ; Designer giữ phần bố cục |
| `UI/` | Màu sắc, thành phần dùng chung, tổng quan, lịch tải lại dữ liệu |
| `Models/` | Kiểu dữ liệu phòng, khách, nhân viên và người đăng nhập |
| `Services/` | Đăng nhập/phiên và thao tác nghiệp vụ |
| `Infrastructure/` | Đọc cấu hình, HTTP, xử lý lỗi Supabase |
| `supabase/migrations/` | Schema, quyền truy cập, giao dịch nhận/trả phòng |
| `supabase/tests/` | Kiểm thử quyền và nghiệp vụ database, rollback dữ liệu thử |
| `scripts/` | Cấu hình, build, kiểm thử, chuyển dữ liệu cũ |
| `tests/` | Kiểm thử HTTP mô phỏng và bố cục WinForms |

Khi thêm tính năng: thêm model nếu cần → service gọi API → View hiển thị; luật cần bảo đảm cho nhiều máy đặt ở database. Không viết truy vấn database hoặc HTTP trực tiếp trong Designer/click handler.

## Chức năng và quyền

- `admin`: quản lý phòng, khách, hồ sơ nhân viên và tạo thêm admin từ app. Mọi admin có quyền ngang nhau.
- `receptionist`: quản lý phòng, nhận/trả phòng và khách hàng.
- Auth user chưa được cấp `profiles`, hoặc bị đặt `is_active = false`, không được truy cập dữ liệu khách sạn.
- Hồ sơ nhân sự tách khỏi tài khoản Auth; không có cột mật khẩu trong bảng nhân viên.
- Nhận phòng khóa bản ghi phòng và có chỉ mục chống hai lượt lưu trú đang hoạt động trên một phòng. Trả phòng kiểm tra ngày trong cùng giao dịch.
- Giá lưu trú được chụp tại lúc nhận phòng. Trạng thái phòng được tính từ lượt lưu trú, không lưu hai cột dễ lệch nhau.
- Số điện thoại là chuỗi để giữ số 0 đầu.
- Màn hình đang mở cập nhật mỗi 15 giây; thay đổi trong cùng app cập nhật ngay qua sự kiện. Đây là polling, chưa dùng Realtime WebSocket.
- Thao tác ghi không được tự retry khi lỗi mạng. Tải lại và kiểm tra kết quả trước khi gửi lại.

Xem [tạo admin từ app](docs/ADMIN_ACCOUNTS.md).

## Kiểm thử

```powershell
.\scripts\Test-Supabase.ps1
.\scripts\Test-LegacyConversion.ps1
.\scripts\Test-ClientUi.ps1
```

Các kiểm thử trên chạy offline, không cần tài khoản cloud. Chúng không thay thế thử nghiệm trên project thật. Sau khi thiết lập, chạy `supabase/tests/permissions_and_stays.sql` trong SQL Editor và làm checklist hai client trong hướng dẫn.

## Trạng thái chuyển đổi

Project Supabase đã hoạt động, schema và hàm tạo admin đã triển khai. Tài khoản `debadao113@gmail.com` là admin như các admin khác; không có cấp cao hơn. Dữ liệu cũ chưa được nhập sang database mới. Hướng dẫn chuyển dữ liệu và kiểm tra nằm trong [SUPABASE_SETUP.md](docs/SUPABASE_SETUP.md).

Các chức năng Google/OTP, QR thanh toán, chat lễ tân, AI và email xác nhận chưa được triển khai ở lần tái cấu trúc này. Nền tảng Auth, PostgreSQL và HTTPS đã tách riêng để mở rộng sau.
