# NT106.R11-Nhom4
NT106 .R11- Nhóm 4 

## HotelServer

Project WinForms độc lập, sử dụng .NET Framework 4.7.2. Bước đầu đã có cửa sổ
hiển thị và kiểm tra cấu hình địa chỉ, cổng. TCP listener sẽ được bổ sung ở bước tiếp theo.

Mở solution trong Visual Studio hỗ trợ `.slnx`, chọn `HotelServer` làm Startup Project và chạy.
Cấu hình trong `HotelServer/App.config`, mặc định địa chỉ `0.0.0.0`, cổng `8888`.

Có thể build riêng khi máy đã có .NET SDK và .NET Framework 4.7.2 Developer Pack.

```powershell
dotnet msbuild HotelServer/HotelServer.csproj /t:Build /p:Configuration=Debug
```
