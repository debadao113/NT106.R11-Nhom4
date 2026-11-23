# NT106.R11-Nhom4
NT106 .R11- Nhóm 4 

## Cài project trên máy thành viên

Xem [hướng dẫn đồng bộ môi trường nhóm](docs/TEAM_SETUP.md). Mở
`ChuuongTrinhQuanLyKhachSan.sln` để dùng với Visual Studio 2022.
Chạy `scripts/Setup-Team.ps1` để tạo cấu hình riêng cho máy và
`scripts/Build-Team.ps1` để restore đúng phiên bản Guna rồi build cả hai ứng dụng.
Cấu hình local và tài khoản trong database không được đồng bộ qua Git.

## HotelServer

Project WinForms độc lập, sử dụng .NET Framework 4.7.2. Nhấn **Khởi động** để lắng nghe
TCP và **Dừng** để giải phóng cổng. Đóng cửa sổ cũng giải phóng cổng. Nếu cổng đang
được sử dụng, cửa sổ hiển thị lỗi và cho phép thử khởi động lại.

Server nhận tối đa 100 client đồng thời bằng các vòng đọc bất đồng bộ. Cửa sổ hiển thị
client đang kết nối và tối đa 500 dòng nhật ký gần nhất. Khi dừng, server đóng và chờ
dọn xong các kết nối trước khi cho khởi động lại.

Mở solution trong Visual Studio hỗ trợ `.slnx`, chọn `HotelServer` làm Startup Project và chạy.
Cấu hình trong `HotelServer/App.config`, mặc định địa chỉ `0.0.0.0`, cổng `8888`.

Có thể build riêng khi máy đã có .NET SDK và .NET Framework 4.7.2 Developer Pack.

```powershell
dotnet msbuild HotelServer/HotelServer.csproj /t:Build /p:Configuration=Debug
```

Sau khi chạy và nhấn **Khởi động**, kiểm tra trên cùng máy bằng PowerShell.

```powershell
Test-NetConnection 127.0.0.1 -Port 8888
```

`TcpTestSucceeded` là `True` khi đang lắng nghe và `False` sau khi nhấn **Dừng**.
Khi kiểm tra qua LAN, dùng địa chỉ IP của máy chạy HotelServer.

### Giao thức TCP phiên bản 1

Mỗi gói là một JSON UTF-8 kết thúc bằng xuống dòng `\n`, tối đa 1 MiB trước ký tự
xuống dòng. Client tự tạo `requestId` riêng cho mỗi yêu cầu. Server trả lại cùng mã
trong phản hồi. Gói sai cú pháp nhận `INVALID_REQUEST`, lệnh chưa hỗ trợ nhận
`UNKNOWN_ACTION`. Gói quá lớn làm đóng kết nối sau khi báo lỗi.

```json
{"type":"request","requestId":"1","action":"HELLO","data":null}
{"type":"request","requestId":"2","action":"PING","data":null}
```

Phản hồi có `type=response`, `requestId`, `action`, `success`, `data`, `error`.
`HELLO` trả `connectionId` và `protocolVersion`. `PING` trả `serverTimeUtc`.
Log chỉ ghi kết quả xử lý, tránh ghi nội dung dữ liệu của yêu cầu.

### Kết nối từ WinForms

Màn hình đăng nhập và dashboard hỗ trợ kéo đổi kích thước, thu nhỏ và phóng to bằng
thanh tiêu đề Windows. Thanh chức năng dashboard tự xuống dòng khi cửa sổ hẹp.
Vùng nghiệp vụ có thanh cuộn để truy cập đầy đủ các trường khi mở nhiều cửa sổ cạnh nhau.

Trong dashboard, nhấn **Kết nối server** rồi **Kết nối / PING** để thử kết nối.
Chỉnh `HotelServerHost` và `HotelServerPort` trong `App.config` của ứng dụng khách.
NetworkClient dùng chung một kết nối, ghép phản hồi bằng `requestId`, giới hạn kết nối
5 giây, yêu cầu thông thường 10 giây và nghiệp vụ dữ liệu 90 giây để chờ Azure SQL khởi động lại. Sau khi mất kết nối hoặc timeout, nhập lại tài khoản và
mật khẩu trong cửa sổ **Kết nối server**, nhấn **Đăng nhập lại** để tiếp tục.
Các thao tác ghi dữ liệu cần kiểm tra kết quả hiện có trước khi gửi lại sau timeout.

### Dữ liệu phòng qua server

`GET_ROOMS` trả danh sách phòng theo `roomid`. Màn hình Thêm phòng tự kết nối để tải
danh sách và có nút **Tải lại phòng** nếu server chưa sẵn sàng. Cấu hình SQL Server
của HotelServer nằm trong `HotelServer/App.config`. Khi nhận yêu cầu dữ liệu đầu tiên,
server tạo database và các bảng còn thiếu, dùng chung mã khởi tạo với ứng dụng cũ.
Lỗi truy cập SQL được trả bằng `DATABASE_UNAVAILABLE`.

`ADD_ROOM` nhận `data` gồm `roomNo`, `roomType`, `bed`, `price`. Server kiểm tra
độ dài tương ứng 50/100/50 ký tự, các trường có nội dung và giá là số nguyên không âm.
Lưu bằng SQL có tham số. Trùng số phòng trả `ROOM_EXISTS`, dữ liệu sai trả
`VALIDATION_ERROR`. Màn hình Thêm phòng gửi thao tác lưu qua server và tải lại danh sách.

Toàn bộ màn hình WinForms truy cập dữ liệu qua HotelServer. Client không khởi tạo
database, không có chuỗi kết nối SQL và không cần cài SQL Server.

### Đồng bộ danh sách phòng

Sau khi `ADD_ROOM` lưu thành công, server phát gói `type=event`, `action=ROOMS_CHANGED`
cho các client đang kết nối. Màn hình Thêm phòng tự tải danh sách mới, kể cả khi yêu cầu
tải trước đó đang chạy. Khi mất kết nối, bảng được xóa để tránh dùng dữ liệu cũ. Khi
kết nối và đăng nhập lại tại cửa sổ kết nối, danh sách được lấy lại.
Để thử, chạy HotelServer và hai phiên ứng dụng, thêm phòng ở A rồi quan sát B cập nhật.

Đăng ký khách/trả phòng phát `ROOMS_CHANGED` và `CUSTOMERS_CHANGED`; thay đổi nhân viên
phát `EMPLOYEES_CHANGED`. Trang đang mở tự tải lại; trang ẩn tải lại khi được mở.
Thay đổi bằng SSMS ngoài ứng dụng chưa có thông báo tự động.
TCP không mã hóa chỉ dành cho LAN thử nghiệm. Khi triển khai Internet, bật TLS
như hướng dẫn bên dưới trước khi mở cổng cho client.

### HotelServer trên Azure VM với TLS

Giữ Azure SQL riêng với máy ảo. Cấu hình mẫu `HotelServer/App.Azure.config` bật
`DatabaseInitializationMode=Existing`: chương trình kiểm tra bốn bảng đã migrate,
không tạo database/bảng qua `master`. Địa chỉ `.database.windows.net` cũng tự dùng
chế độ kiểm tra này. `scripts/Configure-AzureDatabase.ps1` yêu cầu nhập mật khẩu SQL
trực tiếp trên VM, kiểm tra kết nối, rồi lưu cấu hình với quyền đọc giới hạn.
Không commit file cấu hình thực tế có mật khẩu.

1. Đóng HotelServer trên VM, thay `HotelServer.exe` bằng bản Release mới; giữ
   `HotelServer.exe.config` đang có mật khẩu Azure SQL.
2. Chép `scripts/Configure-ServerTls.ps1` vào `C:\HotelServer`, chạy bằng cùng tài
   khoản Windows mở HotelServer:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File C:\HotelServer\Configure-ServerTls.ps1
   ```

   Script tạo hoặc dùng lại chứng chỉ `hotelserver` trong `CurrentUser/My`,
   private key không xuất được, hạn ba tháng; bật TLS mà giữ cấu hình SQL.
3. Mở HotelServer, nhấn **Khởi động**, xác nhận trạng thái **TLS 1.2**. Khi ngắt RDP,
   không Sign out vì server vẫn là ứng dụng WinForms trong phiên người dùng.
4. Chép **chứng chỉ công khai** `HotelServer-public.cer` từ VM về máy client bằng
   kết nối RDP đã xác thực. Không xuất/gửi private key. Đối chiếu SHA-256 trên VM
   với giá trị script cấu hình client hiển thị:

   ```powershell
   .\Configure-ClientTls.ps1 -CertificatePath .\HotelServer-public.cer -ClientDirectory .
   ```

   Cấu hình client dùng `HotelServerHost=20.198.74.207`, `HotelServerPort=8888`,
   `TransportSecurity=Tls12`, `TlsTargetHost=hotelserver`, `TlsCertificateSha256`
   là SHA-256 của đúng chứng chỉ. Không có cơ chế tự hạ xuống TCP khi TLS lỗi.
   Client xác minh pin, tên và thời hạn chứng chỉ; chứng chỉ tự ký được tin cậy
   qua pin được phân phối riêng, không cần thêm vào Trusted Root của Windows.
5. Sau đó thêm rule Azure NSG và Windows Firewall cho TCP 8888. Giai đoạn kiểm tra
   chỉ cho IP công cộng của máy thử nghiệm; cổng RDP 3389 giữ rule My IP riêng.
6. Chạy `HotelServerProbe.exe` để kiểm tra TLS, PING, LOGIN và GET_ROOMS. Công cụ này
   và ứng dụng WinForms đầy đủ đều không cần SQL local. Xem hướng dẫn cloud phía dưới.

Khi thay chứng chỉ hoặc hết hạn, chạy lại script trên VM và phân phối lại CER/pin
cho client. Bản TLS không phải Windows Service và chưa tự khởi động theo Windows.

### Tài khoản lễ tân

HotelServer có nút **Tài khoản lễ tân** để xem danh sách và tạo tài khoản trong SQL
Server. Mật khẩu được lưu bằng PBKDF2-HMAC-SHA256 với salt ngẫu nhiên riêng từng tài
khoản. Cửa sổ tạo tài khoản chỉ nằm trên ứng dụng server tại máy chủ.

App WinForms gửi lệnh `LOGIN` với `data` gồm `username` và `password` để đăng nhập.
Server xác thực từ bảng `staff_accounts`, gắn tài khoản vào kết nối hiện tại và trả
thông tin tài khoản gồm mã, tên đăng nhập và tên hiển thị. `GET_ROOMS`, `ADD_ROOM` và
thông báo `ROOMS_CHANGED` yêu cầu kết nối đã đăng nhập. Thiếu đăng nhập trả
`AUTH_REQUIRED`, sai tài khoản hoặc mật khẩu trả `INVALID_CREDENTIALS`. Sau 5 lần
đăng nhập sai trên một kết nối, server đóng kết nối đó. Kết nối lại cần xác thực lại.

Danh sách client trên server hiển thị tên tài khoản để phân biệt các phiên lễ tân.
Mỗi máy cài mới cần tạo tài khoản qua cửa sổ server sau khi cấu hình database.
Các chức năng tài khoản khách, Google, OTP và phân quyền admin thuộc các mốc sau.

### Tích hợp chi tiết khách hàng và nhân viên

Dashboard đã nối hai trang **Chi tiết khách hàng** và **Nhân viên**. Trang khách hàng
có bộ lọc tất cả, đang lưu trú và đã trả phòng, hỗ trợ cả dữ liệu cũ có checkout NULL
và dữ liệu hiện dùng NO/YES. Trang nhân viên hỗ trợ đăng ký, danh sách và xóa hồ sơ.
Bảng `employee` được tạo tự động khi khởi tạo cơ sở dữ liệu. Mật khẩu nhân viên mới
được băm PBKDF2-SHA256 và không xuất hiện trong danh sách; dữ liệu cũ không bị viết lại.

Hai trang này gọi HotelServer và dùng database đã cấu hình ở server.
Hồ sơ `employee` và tài khoản đăng nhập HotelServer trong `staff_accounts` hiện quản lý
riêng. Thêm nhân viên tại dashboard chưa tạo tài khoản đăng nhập server; hãy dùng
**Tài khoản lễ tân** trên HotelServer để cấp tài khoản truy cập.

Đã kiểm tra tích hợp trên cơ sở dữ liệu tạm: khởi tạo lặp lại, chuyển trang, ba bộ lọc
khách, thêm nhân viên có dấu nháy trong tên, băm mật khẩu, xóa nhân viên, tải danh sách
và thu nhỏ cửa sổ. Kiểm tra đăng nhập nhiều client và đồng bộ phòng cũng đã qua.

## Chạy client với Azure SQL hoàn toàn trên cloud

Luồng: **WinForms → TLS/TCP 8888 → HotelServer trên Azure VM → Azure SQL**.
Chỉ VM giữ thông tin đăng nhập SQL. Các máy thành viên không cần SQL Server/SSMS.
IP máy client cần được phép ở cả Azure NSG và Windows Firewall của VM cho TCP 8888;
RDP 3389 chỉ dành cho quản trị viên. Khi đổi Wi-Fi, cập nhật rule cho IP mới.

Sau khi lấy bản code có thay đổi này, lấy CER công khai từ quản trị viên và chạy:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Setup-CloudClient.ps1 -CertificatePath 'C:\duong-dan\HotelServer-public.cer'
powershell -NoProfile -File .\scripts\Build-Team.ps1 -Configuration Release
.\bin\Release\ChuuongTrinhQuanLyKhachSan.exe
```

Script tạo/cập nhật `App.local.config` chỉ cho client, bật TLS và bỏ chuỗi kết nối SQL
cũ nếu có. Không cần chạy Setup-Team hoặc Initialize-TeamDatabase khi chỉ dùng cloud.
Nếu dùng gói client đã đóng sẵn, giải nén và mở exe cùng thư mục `Guna.UI2.dll` và
`ChuuongTrinhQuanLyKhachSan.exe.config`; không cần Visual Studio.

Các lệnh nghiệp vụ đều yêu cầu đăng nhập:

| Lệnh | Dữ liệu |
| --- | --- |
| GET_CUSTOMERS | `{status:"all"/"active"/"checkedout",name:"tiền tố tên"}` |
| REGISTER_CUSTOMER | roomid, cname, mobile, nationality, gender, dob, idproof, address, checkin |
| CHECKOUT_CUSTOMER | cid, date |
| GET_EMPLOYEES | không có |
| ADD_EMPLOYEE | ename, mobile, gender, emailid, username, password |
| DELETE_EMPLOYEE | eid |

Ngày gửi từ client theo `yyyy-MM-dd`. Server kiểm tra dữ liệu, dùng SQL có tham số,
khóa phòng và giao dịch khi đăng ký/trả phòng. Nhân viên được băm mật khẩu tại server;
danh sách trả về không có mật khẩu/hash. Nút trả phòng chỉ cập nhật lưu trú, chưa phải
xác nhận thanh toán QR. Phân quyền admin/khách và cổng thanh toán thuộc kế hoạch sau.
Không tự gửi lại thao tác ghi khi timeout; kiểm tra dữ liệu trước khi thử lại.

Kiểm thử dành cho người phát triển:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-TlsTransport.ps1
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-HotelOperations.ps1
```

Kiểm thử nghiệp vụ tạo rồi xóa database tạm có tên ngẫu nhiên trên SQL Server localhost;
không dùng hoặc sửa dữ liệu Azure. Chỉ máy chạy kiểm thử này mới cần SQL local.
