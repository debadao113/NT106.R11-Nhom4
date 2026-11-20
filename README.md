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

Màn hình đăng nhập và dashboard hỗ trợ kéo đổi kích thước, thu nhỏ và phóng to bằng
thanh tiêu đề Windows. Thanh chức năng dashboard tự xuống dòng khi cửa sổ hẹp.
Vùng nghiệp vụ có thanh cuộn để truy cập đầy đủ các trường khi mở nhiều cửa sổ cạnh nhau.

Trong dashboard, nhấn **Kết nối server** rồi **Kết nối / PING** để thử kết nối.
Chỉnh `HotelServerHost` và `HotelServerPort` trong `App.config` của ứng dụng khách.
NetworkClient dùng chung một kết nối, ghép phản hồi bằng `requestId`, giới hạn kết nối
5 giây và yêu cầu 10 giây. Sau khi mất kết nối hoặc timeout, nhập lại tài khoản và
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

Các màn hình đăng ký khách và trả phòng hiện vẫn dùng kết nối SQL trong `App.config`
của ứng dụng WinForms. Trong giai đoạn tích hợp, cấu hình hai ứng dụng trỏ cùng database.

### Đồng bộ danh sách phòng

Sau khi `ADD_ROOM` lưu thành công, server phát gói `type=event`, `action=ROOMS_CHANGED`
cho các client đang kết nối. Màn hình Thêm phòng tự tải danh sách mới, kể cả khi yêu cầu
tải trước đó đang chạy. Khi mất kết nối, bảng được xóa để tránh dùng dữ liệu cũ. Khi
kết nối và đăng nhập lại tại cửa sổ kết nối, danh sách được lấy lại.
Để thử, chạy HotelServer và hai phiên ứng dụng, thêm phòng ở A rồi quan sát B cập nhật.

Các thay đổi từ màn hình đăng ký khách, trả phòng hoặc thao tác SQL trực tiếp hiện cần
nhấn **Tải lại phòng**. Các màn hình này sẽ được chuyển sang server ở phần tích hợp sau.
TCP hiện dành cho môi trường LAN thử nghiệm, dữ liệu truyền qua TCP chưa có TLS.

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

Hai trang này đang truy cập SQL theo `HotelConnection` trong App.config của client.
Hồ sơ `employee` và tài khoản đăng nhập HotelServer trong `staff_accounts` hiện quản lý
riêng. Thêm nhân viên tại dashboard chưa tạo tài khoản đăng nhập server; hãy dùng
**Tài khoản lễ tân** trên HotelServer để cấp tài khoản truy cập.

Đã kiểm tra tích hợp trên cơ sở dữ liệu tạm: khởi tạo lặp lại, chuyển trang, ba bộ lọc
khách, thêm nhân viên có dấu nháy trong tên, băm mật khẩu, xóa nhân viên, tải danh sách
và thu nhỏ cửa sổ. Kiểm tra đăng nhập nhiều client và đồng bộ phòng cũng đã qua.
