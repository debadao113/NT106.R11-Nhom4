# NT106.R11-Nhom4
NT106 .R11- Nhóm 4 

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

Trong dashboard, nhấn **Kết nối server** rồi **Kết nối / PING** để thử kết nối.
Chỉnh `HotelServerHost` và `HotelServerPort` trong `App.config` của ứng dụng khách.
NetworkClient dùng chung một kết nối, ghép phản hồi bằng `requestId`, giới hạn kết nối
5 giây và yêu cầu 10 giây. Sau khi mất kết nối hoặc timeout, dùng nút kết nối để thử lại.
Các thao tác ghi dữ liệu cần kiểm tra kết quả hiện có trước khi gửi lại sau timeout.

### Dữ liệu phòng qua server

`GET_ROOMS` trả danh sách phòng theo `roomid`. Màn hình Thêm phòng tự kết nối để tải
danh sách và có nút **Tải lại phòng** nếu server chưa sẵn sàng. Cấu hình SQL Server
của HotelServer nằm trong `HotelServer/App.config`. Khi nhận yêu cầu dữ liệu đầu tiên,
server tạo database và các bảng còn thiếu, dùng chung mã khởi tạo với ứng dụng cũ.
Lỗi truy cập SQL được trả bằng `DATABASE_UNAVAILABLE`.

Các màn hình đăng ký khách và trả phòng hiện vẫn dùng kết nối SQL trong `App.config`
của ứng dụng WinForms. Trong giai đoạn tích hợp, cấu hình hai ứng dụng trỏ cùng database.
