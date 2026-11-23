# Cài và chạy chung cho nhóm NT106

## 1. Thống nhất môi trường

| Thành phần | Cấu hình chung |
| --- | --- |
| Hệ điều hành | Windows 10/11 |
| IDE | Visual Studio 2022 trở lên, workload **.NET desktop development** |
| Framework | **.NET Framework 4.7.2 Developer Pack/Targeting Pack** |
| Thư viện giao diện | **Guna.UI2.WinForms 2.0.3.5**, restore từ NuGet |
| Database | Azure SQL qua HotelServer; SQL local chỉ dành cho server phát triển riêng |
| Nhánh tích hợp | `develop` |
| Solution nên mở | `ChuuongTrinhQuanLyKhachSan.sln` |

Máy client cloud không cần SSMS hoặc SQL Server Database Engine. Máy chạy server phát triển riêng mới cần SQL local.
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

## 3. Chạy server riêng để phát triển (tùy chọn)

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

## 5. Cả nhóm dùng chung Azure SQL

Toàn bộ nghiệp vụ đã đi qua HotelServer. Máy client chỉ cần cấu hình host, cổng và
pin chứng chỉ TLS, không cài SQL Server và không giữ mật khẩu Azure SQL.
Xem phần **Chạy client với Azure SQL hoàn toàn trên cloud** trong [README](../README.md).

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Setup-CloudClient.ps1 -CertificatePath 'C:\duong-dan\HotelServer-public.cer'
powershell -NoProfile -File .\scripts\Build-Team.ps1 -Configuration Release
```

Không chạy Initialize-TeamDatabase trên máy chỉ dùng cloud. Quản trị viên cần cho
phép IP Internet của từng máy trong NSG và Windows Firewall cổng 8888. Các tài khoản
lễ tân nằm trong database dùng chung; không tạo lại tài khoản trên từng máy.

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
| Dữ liệu hai máy khác nhau | Đối chiếu cùng commit, HotelServerHost, pin TLS và server đang chạy |
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
