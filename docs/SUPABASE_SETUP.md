# Thiết lập Supabase cho HotelDesk

## Project của nhóm

- Tổ chức: `Nhom4` (Free).
- Project: `NT106-Nhom4-HotelDesk`, ref `gdsoaqpiwbuwwnceksjt`.
- URL: `https://gdsoaqpiwbuwwnceksjt.supabase.co`.
- Khu vực thực tế: Tokyo (`ap-northeast-1`).
- `App.config` đã có URL và publishable key dùng chung. Thành viên dùng đúng project này chỉ cần build, không phải chạy lại script cấu hình.
- Schema và hàm tạo admin đã triển khai; đăng nhập bằng admin đã cấp để tạo thêm tài khoản ngay trong app. Xem `docs/ADMIN_ACCOUNTS.md`.

## 1. Tạo project dùng chung

1. Mở https://supabase.com/dashboard và tạo project trong tài khoản của nhóm.
2. Chọn gói Free nếu phù hợp; tự đặt và giữ mật khẩu database. Không đưa mật khẩu vào mã nguồn.
3. Với project mới, chạy `001_hotel.sql` rồi `003_admin_accounts.sql` trong SQL Editor. Không chạy lại trên project nhóm hiện tại. Dự án chỉ giữ migration schema và migration quyền admin hiện tại.
4. Lấy Project URL và publishable key tại phần API của project. Có thể dùng anon key cũ; không dùng `service_role` hoặc secret key.

Project URL và public key xác định project, không cấp quyền quản trị. Quyền đọc/ghi được kiểm soát bởi JWT của người đăng nhập, RLS và các hàm nghiệp vụ.

## 2. Tạo tài khoản đầu tiên

Trong Authentication → Users, dùng chức năng thêm người dùng để tạo tài khoản bằng **email thật và mật khẩu mới**. Không dùng lại tài khoản mẫu `nhom4 / 123`.

Sau đó chạy đoạn dưới trong SQL Editor, thay email và tên cho đúng. Nó chỉ cấp quyền cho user đã tồn tại trong Supabase Auth:

```sql
insert into public.profiles(user_id, email, display_name, role)
select id, email, 'Quản trị nhóm 4', 'admin'
from auth.users where lower(email) = lower('EMAIL_CUA_BAN')
on conflict (user_id) do update
set display_name = excluded.display_name, role = excluded.role, is_active = true;
```

Kiểm tra query thêm được **1 dòng**. Nếu 0 dòng, email không khớp hoặc chưa tạo Auth user.

Lặp lại với thành viên khác, đổi `role` thành `receptionist` nếu chỉ cần quản lý phòng/khách. Tạo Auth user không tự cấp quyền vào dữ liệu khách sạn. Không có trigger nào tin vào `user_metadata` để cấp quyền.

Hồ sơ ở bảng `employees` chỉ phục vụ quản lý nhân sự. Thêm/xóa hồ sơ nhân viên **không tạo/xóa tài khoản đăng nhập**. Muốn khóa đăng nhập vào dữ liệu, đặt `profiles.is_active = false`; muốn thu hồi phiên đăng nhập, thực hiện trong Supabase Auth. Tài khoản quản trị mới quản lý được hồ sơ nhân viên.

## Đăng nhập bằng tên nhom4 và Long

App hỗ trợ cả tên đăng nhập và email. Tên nhân viên được chuẩn hóa về chữ thường và ánh xạ tới email kỹ thuật riêng, không cần mở API tra cứu email cho người chưa đăng nhập.

| Nhập trên app | Tạo trong Supabase Auth | Quyền sau khi chạy SQL |
| --- | --- | --- |
| `nhom4` | `nhom4@staff.hoteldesk.invalid` | admin |
| `Long` hoặc `long` | `long@staff.hoteldesk.invalid` | admin |

1. Đăng nhập app bằng một admin đang hoạt động, mở **Tài khoản admin**.
2. Nhập tên `nhom4` hoặc `Long`, tên hiển thị và mật khẩu tự đặt, rồi bấm **Tạo admin**. Không cần tạo trước trong Dashboard hoặc chạy SQL riêng.
3. Đăng nhập bằng `nhom4` hoặc `Long` và mật khẩu tương ứng đã đặt. Không có mật khẩu `123` gán trong mã nguồn; app không sửa, nối thêm ký tự hoặc thay đổi mật khẩu người dùng nhập.

Địa chỉ `.invalid` là email kỹ thuật không nhận thư: không dùng để gửi OTP, xác nhận hay khôi phục mật khẩu qua email. Quản trị viên phải quản lý mật khẩu hai tài khoản này trong Supabase Dashboard. Tài khoản khách hàng và người cần khôi phục qua email nên dùng email thật; cách đăng nhập email hiện tại vẫn hoạt động.

## 3. Cấu hình máy thành viên

Trong PowerShell tại thư mục dự án:

```powershell
.\scripts\Setup-Supabase.ps1 -ProjectUrl "https://PROJECT.supabase.co" -PublishableKey "sb_publishable_THAY_BANG_KEY_THAT"
.\scripts\Build-Team.ps1 -Configuration Debug
```

Script ghi `App.local.config`, được Git bỏ qua. Visual Studio dùng file này khi build. Nếu chỉ sửa config, cần build lại trước khi chạy. Dùng cùng URL/key cho cả nhóm để dùng cùng database; không có danh sách IP cần thêm thủ công.

Mở solution và đặt `ChuuongTrinhQuanLyKhachSan` làm Startup Project. Chỉ còn một project WinForms; đăng nhập bằng email/mật khẩu vừa tạo. Máy thành viên không cần SQL Server, máy ảo hay server TCP riêng.

Muốn phát hành bản exe cho thành viên: gửi cả nội dung `bin/Release`, gồm exe, Guna.UI2.dll và file `.exe.config` sinh ra từ cấu hình đúng. Không gửi riêng exe.

## 4. Kiểm tra trước khi dùng chung

1. Admin đăng nhập: thêm phòng, tải lại và thấy phòng mới.
2. Mở hai app, đăng nhập bằng hai tài khoản. Một máy nhận phòng; máy kia sẽ cập nhật trong khoảng 15 giây khi đang mở màn hình đó. Chuyển trang hoặc bấm tải lại để lấy dữ liệu ngay. Đây là **polling**, chưa dùng Supabase Realtime.
3. Hai máy cùng nhận một phòng: chỉ một thao tác được thành công; thao tác còn lại báo phòng đã được nhận.
4. Trả phòng: ngày không được trước ngày nhận; phòng trở về trạng thái trống.
5. Lễ tân không truy cập được quản lý nhân viên; API cũng chặn thao tác nhân sự dù gọi trực tiếp.
6. User Auth chưa có profile hoặc profile không hoạt động không được đăng nhập vào app.
7. Chạy kiểm thử database ở `supabase/tests/permissions_and_stays.sql` trong SQL Editor. Script dùng dữ liệu thử trong một transaction rồi rollback, không giữ lại dữ liệu thử.

Đổi Wi-Fi không cần cấu hình lại IP. Nếu project đang tạm dừng hoặc mất mạng, app báo lỗi và cho tải lại; không tự động gửi lại thao tác ghi, vì thao tác trước có thể đã được xử lý.

## 5. Chuyển dữ liệu đang có

Mã nguồn mới đã bỏ phụ thuộc Azure, nhưng **chưa chuyển dữ liệu thật và chưa xóa tài nguyên Azure**. Project Supabase phải tồn tại trước khi làm bước này.

Trước khi ngừng hệ thống cũ:

1. Tạm dừng các thao tác ghi trên app cũ; xuất một bản sao lưu cuối cùng của dữ liệu hiện có, giữ bên ngoài Git.
2. Dùng `scripts/Convert-LegacyData.ps1` với ba file JSON xuất từ các bảng cũ `rooms`, `customer`, `employee` để tạo SQL nhập dữ liệu. Xem hướng dẫn trong phần đầu script. Script không kết nối Azure hay lưu mật khẩu.
3. Chạy SQL được tạo trên Supabase sau migration. Nó giữ mã phòng/lưu trú/nhân viên, chuyển ngày tháng, giữ giá tại thời điểm chuyển và đặt lại bộ đếm ID.
4. Đối chiếu số dòng, phòng đang có khách và thử nhận/trả trên dữ liệu thử. Nếu trạng thái `booked` cũ không khớp lượt lưu trú, script dừng để nhóm xử lý trước khi nhập.
5. Tạo tài khoản mới qua Auth; không nhập hash/mật khẩu SQL Server cũ vào Supabase Auth.
6. Chuyển cả nhóm sang bản app mới, kiểm tra lại, sau đó mới ngừng/xóa dịch vụ cloud cũ theo lựa chọn của chủ tài khoản. Không chạy hai hệ thống để ghi cùng lúc vì chúng dùng hai database độc lập.

Tên cũ → tên mới: `rooms` → `rooms`; `customer` → `stays`; `employee` → `employees`; `staff_accounts` → Supabase Auth + `profiles`.

## Tài liệu chính thức

- [Supabase Auth](https://supabase.com/docs/guides/auth)
- [REST API](https://supabase.com/docs/guides/api)
- [Row Level Security](https://supabase.com/docs/guides/database/postgres/row-level-security)
- [API keys](https://supabase.com/docs/guides/getting-started/api-keys)
- [Database functions](https://supabase.com/docs/guides/database/functions)
