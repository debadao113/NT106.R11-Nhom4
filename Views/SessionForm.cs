using System;
using System.Drawing;
using System.Windows.Forms;
using HotelData;
using Guna.UI2.WinForms;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan
{
    public sealed class SessionForm : Form
    {
        private readonly Label status = new Label
        {
            Name = "ConnectionStatus",
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ForeColor = UiTheme.Muted
        };
        private readonly Guna2Button connect = UiTheme.Button("Kiểm tra kết nối");
        public SessionForm()
        {
            Text = "Kết nối Supabase";
            ClientSize = new Size(660, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 8)
            };
            var disconnect = UiTheme.Button("Đăng xuất");
            var login = UiTheme.Button("Đăng nhập lại", true);
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(20, 0, 20, 0)
            };
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var email = new Guna2TextBox
            {
                Name = "ServerUsername",
                Dock = DockStyle.Fill,
                BorderRadius = 7,
                BorderColor = UiTheme.Line,
                Margin = new Padding(0, 6, 0, 8),
                MaxLength = 254,
                Text = LoginIdentity.DisplayName(HotelSession.CurrentAccount?.Email) ?? ""
            };
            var password = new Guna2TextBox
            {
                Name = "ServerPassword",
                Dock = DockStyle.Fill,
                BorderRadius = 7,
                BorderColor = UiTheme.Line,
                Margin = new Padding(0, 6, 0, 8),
                MaxLength = 128,
                UseSystemPasswordChar = true
            };
            fields.Controls.Add(new Label { Text = "Tài khoản", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Muted }, 0, 0);
            fields.Controls.Add(email, 1, 0);
            fields.Controls.Add(new Label { Text = "Mật khẩu", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Muted }, 0, 1);
            fields.Controls.Add(password, 1, 1);
            login.Click += async delegate
            {
                connect.Enabled = login.Enabled = disconnect.Enabled = false;
                try
                {
                    var account = await HotelSession.LoginAsync(email.Text.Trim(), password.Text);
                    if (!IsDisposed)
                        status.Text = "Đã đăng nhập: " + account.DisplayName;
                }
                catch (Exception ex)
                {
                    if (!IsDisposed)
                        status.Text = ex.Message;
                }
                finally
                {
                    if (!IsDisposed)
                    {
                        password.Clear();
                        connect.Enabled = login.Enabled = disconnect.Enabled = true;
                    }
                }
            };
            disconnect.Click += async delegate
            {
                connect.Enabled = login.Enabled = disconnect.Enabled = false;
                try
                {
                    await HotelSession.LogoutAsync();
                    status.Text = "Đã đăng xuất.";
                }
                catch (Exception ex)
                {
                    status.Text = "Đã xóa phiên trên máy. " + ex.Message;
                }
                finally
                {
                    if (!IsDisposed)
                        connect.Enabled = login.Enabled = disconnect.Enabled = true;
                }
            };
            connect.Click += async delegate
            {
                connect.Enabled = login.Enabled = disconnect.Enabled = false;
                status.Text = "Đang kết nối và kiểm tra...";
                try
                {
                    await HotelSession.CheckConnectionAsync();
                    if (!IsDisposed)
                        status.Text = "Đã xác thực và truy cập Supabase thành công.";
                }
                catch (Exception ex)
                {
                    if (!IsDisposed)
                        status.Text = "Kết nối thất bại: " + ex.Message;
                }
                finally
                {
                    if (!IsDisposed)
                        connect.Enabled = login.Enabled = disconnect.Enabled = true;
                }
            };
            actions.Controls.Add(connect);
            actions.Controls.Add(disconnect);
            actions.Controls.Add(login);
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            var title = UiTheme.Label("Kết nối hệ thống", 18, true);
            title.Dock = DockStyle.Fill;
            title.Padding = new Padding(20, 18, 0, 0);
            root.Controls.Add(title, 0, 0);
            root.Controls.Add(fields, 0, 1);
            root.Controls.Add(status, 0, 2);
            root.Controls.Add(actions, 0, 3);
            Controls.Add(root);
            status.Text = HotelSession.CurrentAccount != null ? "Đã đăng nhập Supabase. Dữ liệu dùng chung được cập nhật định kỳ mỗi 15 giây." : "Chưa đăng nhập. Nhập tên tài khoản hoặc email và mật khẩu.";
        }
    }
}
