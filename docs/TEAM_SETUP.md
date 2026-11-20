# Cài và chạy chung cho nhóm NT106

## 1. Thống nhất môi trường

| Thành phần | Cấu hình chung |
| --- | --- |
| Hệ điều hành | Windows 10/11 |
| IDE | Visual Studio 2022 trở lên, workload **.NET desktop development** |
| Framework | **.NET Framework 4.7.2 Developer Pack/Targeting Pack** |
| Thư viện giao diện | **Guna.UI2.WinForms 2.0.3.5**, restore từ NuGet |
| Database | SQL Server Developer/Express, Windows Authentication khi chạy độc lập |
| Nhánh tích hợp | `develop` |
| Solution nên mở | `ChuuongTrinhQuanLyKhachSan.sln` |

SSMS chỉ là công cụ quản trị; cần cài SQL Server Database Engine để chạy database.
.NET SDK 8/9/10 và .NET Framework runtime không thay thế targeting pack 4.7.2.
Không nâng riêng framework hoặc phiên bản Guna trên từng máy.

Tài liệu chính thức: [Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472),
[NuGet restore](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore).

## 2. Lấy code cùng phiên bản

Máy mới:

```powershell
git clone --branch develop https://github.com/debadao113/NT106.R11-Nhom4.git
cd NT106.R11-Nhom4
```

Máy đã có project: lưu/commit phần đang làm trước khi đổi nhánh, sau đó:

```powershell
git switch develop
git pull --ff-only origin develop
git rev-parse --short HEAD
```

Cả nhóm đối chiếu cùng mã commit. Nếu `--ff-only` thất bại, kiểm tra lịch sử nhánh,
không dùng `reset --hard` để xóa phần việc chưa tích hợp.

## 3. Mỗi máy chạy độc lập (cách mặc định khi phát triển)

Mở PowerShell tại thư mục repository. Ví dụ SQL Server Express:

```powershell
powershell -NoProfile -File .\scripts\Setup-Team.ps1 -SqlInstance '.\SQLEXPRESS'
powershell -NoProfile -File .\scripts\Build-Team.ps1
powershell -NoProfile -File .\scripts\Test-TeamEnvironment.ps1 -CheckSql
powershell -NoProfile -File .\scripts\Initialize-TeamDatabase.ps1 -Username nhom4 -DisplayName 'Nhom 4'
```

Lệnh cuối hỏi mật khẩu tại máy. Có thể đặt `123` cho tài khoản thử nghiệm theo quy ước
hiện tại của nhóm; không có mật khẩu mặc định được gán trong source. Nếu dùng SQL
instance mặc định, dùng `-SqlInstance '.'`. Nếu dùng LocalDB đã cài, tên thường là
`'(localdb)\MSSQLLocalDB'`. Chọn đúng instance có trên máy, không đoán tên máy người khác.

`Setup-Team.ps1` tạo hai file riêng:

- `App.local.config`: cấu hình client.
- `HotelServer/App.local.config`: cấu hình server.

Các file này bị Git bỏ qua và được dùng thay `App.config` khi build. Script chỉ tạo
file chưa có; chạy lại không ghi đè cấu hình cũ. Muốn đổi instance/IP/cổng, sửa hai file
local tương ứng rồi **build lại**. Khi chạy exe, cấu hình thực tế nằm trong file
`*.exe.config` cạnh exe. Không sửa file sinh ra vì lần build sau sẽ ghi lại.

Khởi tạo database chỉ bổ sung database/bảng còn thiếu theo mã nguồn hiện tại, không
xóa hay đồng bộ bản ghi. Đây chưa phải hệ thống migration nâng cấp các bảng cũ.
Nếu có schema cũ khác định nghĩa, gửi lỗi cụ thể để viết migration giữ dữ liệu.

Tài khoản `nhom4` trên máy của một thành viên không tự xuất hiện trên máy khác.
Script khởi tạo dùng cùng bộ băm mật khẩu của HotelServer, không sao chép mật khẩu
từ bảng `employee`. Có thể tạo thêm tài khoản bằng cửa sổ **Tài khoản lễ tân** trên server.

## 4. Chạy và kiểm tra

```powershell
Start-Process .\HotelServer\bin\Debug\HotelServer.exe
```

Nhấn **Khởi động** trên HotelServer, rồi:

```powershell
powershell -NoProfile -File .\scripts\Test-TeamEnvironment.ps1 -CheckSql -CheckDatabase -CheckServer
Start-Process .\bin\Debug\ChuuongTrinhQuanLyKhachSan.exe
Start-Process .\bin\Debug\ChuuongTrinhQuanLyKhachSan.exe
```

Đăng nhập bằng tài khoản vừa tạo. Thêm một phòng thử ở client A, kiểm tra client B tự
cập nhật. Kiểm tra tiếp đăng ký khách, trả phòng, chi tiết khách và nhân viên.
Đóng app trước khi build lại nếu gặp lỗi exe/dll đang được sử dụng.

Kiểm tra TCP chỉ xác nhận cổng mở; không chứng minh đăng nhập hoặc mọi nghiệp vụ hoạt động.
`-CheckDatabase` kiểm tra các bảng/cột cần có và có tài khoản đang hoạt động; chưa kiểm
tra đầy đủ kiểu dữ liệu, constraint hoặc mật khẩu cụ thể.

## 5. Khi cả nhóm muốn dùng chung dữ liệu

Git đồng bộ mã nguồn. Để dùng chung bản ghi, cần cùng kết nối đến một database.
Hiện tại đăng nhập/thêm phòng đi qua HotelServer, còn đăng ký khách, trả phòng, chi tiết
khách và nhân viên vẫn truy cập SQL trực tiếp. Vì vậy chưa thể chỉ đổi IP server và
coi client là đã dùng chung toàn bộ dữ liệu.

Trong LAN thử nghiệm, muốn dùng chung phải cấu hình cả hai điều kiện:

1. `HotelServerHost` của client trỏ tới IP máy chạy HotelServer; `127.0.0.1` chỉ trỏ về chính máy đang chạy client.
2. `HotelConnection` trong cấu hình client/server cùng trỏ tới SQL instance và database dùng chung; SQL Server cho phép kết nối từ các máy đó và từng máy được cấp quyền phù hợp.

Windows Authentication khi dùng SQL từ xa đòi hỏi tài khoản Windows được SQL Server
chấp nhận. Nếu nhóm dùng SQL Authentication, quản trị viên cấp tài khoản riêng phù hợp,
điền vào `App.local.config`; không đưa mật khẩu vào Git. Không dùng cổng SQL/TCP công khai
trên Internet để thay cho thiết kế kết nối này.

Hướng phát triển tiếp theo: chuyển các nghiệp vụ còn lại sang HotelServer, thống nhất
migration và dữ liệu mẫu. Khi hoàn tất, máy client chỉ cần cấu hình địa chỉ HotelServer.

## 6. Bảng xử lý lỗi thường gặp

| Biểu hiện | Cách kiểm tra/xử lý |
| --- | --- |
| Không mở được `.slnx` | Mở `.sln` vừa bổ sung |
| `MSB3644` hoặc thiếu reference assemblies | Cài Developer Pack/Targeting Pack **4.7.2** |
| Không tìm thấy `Guna.UI2` | Chạy `Build-Team.ps1`, kiểm tra NuGet/network; giữ đúng 2.0.3.5 |
| Không tìm thấy `csc.exe` | Cài workload desktop, dùng script chọn MSBuild có compiler |
| SQL lỗi 26/40/53 | Kiểm tra tên instance, dịch vụ SQL, cấu hình local |
| Login failed cho Windows/SQL | Kiểm tra quyền trên SQL instance đã chọn |
| Cannot open database/Invalid object name | Chạy `Initialize-TeamDatabase.ps1`, kiểm tra đúng database và schema |
| Đăng nhập sai với `nhom4/123` | Tài khoản chưa tạo trên database hiện tại hoặc mật khẩu khác |
| Connection refused/timeout TCP | Bật server, kiểm tra host/cổng, LAN và firewall trên máy server |
| Dữ liệu hai trang khác nhau | So sánh SQL instance/database của client với server |
| Không copy được exe khi build | Đóng các phiên app đang chạy rồi build lại |

Khi báo lỗi, gửi mã commit, thông báo lỗi đầu tiên và kết quả script kiểm tra môi trường.
Không gửi file local chứa mật khẩu hoặc dữ liệu cá nhân của khách.

## 7. Quy trình tích hợp nhóm

- Mỗi tính năng trên `feature/...`, cập nhật từ `develop` trước khi tích hợp.
- Commit source, designer, resx, csproj, thay đổi schema liên quan cùng nhau.
- Không commit `bin`, `obj`, `packages`, `.vs`, file cấu hình local hay database thật.
- Build từ bản tải sạch trước khi gộp vào `develop`; chưa đưa vào `main` ở giai đoạn này.
- Khi thêm project mới, cập nhật cả `.sln` và `.slnx`.
- Workflow GitHub kiểm tra restore/build Debug và Release; chưa thay thế kiểm thử SQL/giao diện.
