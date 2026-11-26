# Tạo tài khoản admin từ app

Dự án chỉ dùng hai quyền: `admin` và `receptionist`. Mọi admin có quyền như nhau, bao gồm tạo thêm admin. Không có tài khoản cao hơn hoặc email được ưu tiên trong mã chạy.

## Sử dụng

1. Đăng nhập app bằng bất kỳ tài khoản `admin` đang hoạt động.
2. Mở **Tài khoản admin** trên sidebar.
3. Điền tên tài khoản hoặc email, tên hiển thị, mật khẩu và xác nhận → **Tạo admin**.
4. Tài khoản mới đăng nhập ở **cùng màn hình Login**, bằng tên/email và mật khẩu vừa đặt. Admin mới cũng mở được **Tài khoản admin** để tạo tiếp admin khác.

Ví dụ: tạo tên `Long` thì hệ thống dùng email kỹ thuật `long@staff.hoteldesk.invalid`; trên app nhập `Long`. Email kỹ thuật không nhận được thư khôi phục. Có thể nhập email thật khi tạo nếu cần địa chỉ nhận thư.

| Quyền | Phòng và khách | Hồ sơ nhân viên | Xem và tạo tài khoản admin |
| --- | --- | --- | --- |
| admin | Có | Có | Có |
| receptionist | Có | Không | Không |

## Thiết lập và nâng cấp

- Project mới: chạy `001_hotel.sql`, rồi `003_admin_accounts.sql`. Tạo admin đầu tiên trong Supabase Auth và cấp profile `admin` theo `SUPABASE_SETUP.md`.
- Project đang chạy bản phân cấp cũ: chỉ chạy `003_admin_accounts.sql`. Migration đổi các hồ sơ quyền cao nhất cũ về `admin`, giữ nguyên mật khẩu, dữ liệu và các tài khoản khác. Script phân cấp cũ đã được đưa ra khỏi dự án; migration 003 vẫn hỗ trợ nâng cấp database đã có.
- Deploy lại Edge Function `create-admin`:

```powershell
npx --yes supabase@3.4.0 functions deploy create-admin --remote=gdsoaqpiwbuwwnceksjt --use-api --yes
```

CLI cần đăng nhập trước bằng `npx --yes supabase@3.4.0 login`. Không dùng `--no-verify-jwt`. Function dùng khóa quản trị chỉ trong môi trường server; không thêm khóa đó vào WinForms/App.config.

Sau khi cập nhật, build lại client và đăng xuất/đăng nhập lại. Function xác minh token qua Supabase Auth, rồi kiểm tra profile có `role = admin` và `is_active = true`; không tin vào role gửi từ client. Tài khoản tạo mới luôn là `admin`.

Luồng: **Login → Auth/profile → màn hình Tài khoản admin → Edge Function → Auth user + profile admin → Login**.

## Kiểm thử

```powershell
.\scripts\Test-Supabase.ps1
.\scripts\Test-ClientUi.ps1
node .\tests\CreateAdminChecks.mjs
```

SQL: chạy `supabase/tests/admin_accounts.sql` trong SQL Editor; dữ liệu thử được rollback. Kiểm thử hai admin có quyền tương đương, lễ tân không xem danh sách tài khoản, tài khoản bị khóa không có quyền và client không tự ghi role.

Auth và bảng profile không dùng chung một giao dịch HTTP. Nếu lưu profile thất bại, function kiểm tra lại rồi hoàn tác chỉ Auth user mới tạo nếu cần. App không tự retry thao tác ghi; khi không xác định được kết quả, kiểm tra danh sách trước khi thử lại. Mật khẩu và khóa server không xuất hiện trong phản hồi hay log của handler.

Tài liệu: [Auth Admin API](https://supabase.com/docs/reference/javascript/auth-admin-createuser), [Edge Function authentication](https://supabase.com/docs/guides/functions/auth-headers).
