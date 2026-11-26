using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan
{
    public sealed class AdminAccountsForm : Form
    {
        private readonly Guna2TextBox username = CreateInput("NewAdminUsername");
        private readonly Guna2TextBox displayName = CreateInput("NewAdminDisplayName");
        private readonly Guna2TextBox password = CreateInput("NewAdminPassword", true);
        private readonly Guna2TextBox confirmation = CreateInput("NewAdminPasswordConfirmation", true);
        private readonly Guna2Button create = UiTheme.Button("Tạo admin", true);
        private readonly Guna2Button reload = UiTheme.Button("Tải lại danh sách");
        private readonly Label status = new Label { Dock = DockStyle.Fill, ForeColor = UiTheme.Muted, AutoEllipsis = true };
        private readonly Guna2DataGridView accounts = new Guna2DataGridView { Dock = DockStyle.Fill };
        private bool busy;

        public AdminAccountsForm()
        {
            Text = "HotelDesk | Quản lý tài khoản admin";
            Name = "AdminAccountsForm";
            ClientSize = new Size(850, 650);
            MinimumSize = new Size(780, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);
            username.MaxLength = 254;
            displayName.MaxLength = 100;
            username.PlaceholderText = "Ví dụ: nhom4, Long hoặc email";
            password.PlaceholderText = "Theo chính sách mật khẩu Supabase";
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(22) };
            foreach (int height in new[] { 40, 48, 150, 52, 48 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(UiTheme.Label("Tài khoản admin", 19, true), 0, 0);
            var hint = UiTheme.Label("Mọi admin đều có thể tạo thêm admin. Tài khoản mới đăng nhập tại màn hình Login như bình thường.", 9);
            hint.Dock = DockStyle.Fill;
            hint.AutoSize = false;
            root.Controls.Add(hint, 0, 1);
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            AddField(fields, 0, 0, "Tên tài khoản hoặc email", username);
            AddField(fields, 1, 0, "Tên hiển thị", displayName);
            AddField(fields, 0, 1, "Mật khẩu", password);
            AddField(fields, 1, 1, "Nhập lại mật khẩu", confirmation);
            root.Controls.Add(fields, 0, 2);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) };
            create.Name = "CreateAdminButton";
            create.Click += CreateAdmin_Click;
            reload.Click += async delegate { await ReloadAsync(); };
            actions.Controls.Add(create);
            actions.Controls.Add(reload);
            root.Controls.Add(actions, 0, 3);
            root.Controls.Add(status, 0, 4);
            UiTheme.StyleGrid(accounts);
            root.Controls.Add(accounts, 0, 5);
            Controls.Add(root);
            Shown += async delegate { await ReloadAsync(); };
        }

        private static Guna2TextBox CreateInput(string name, bool secret = false)
        {
            return new Guna2TextBox { Name = name, Dock = DockStyle.Fill, BorderRadius = 7, BorderColor = UiTheme.Line,
                Font = new Font("Segoe UI", 10), UseSystemPasswordChar = secret, MaxLength = 128 };
        }

        private static void AddField(TableLayoutPanel parent, int column, int row, string caption, Control input)
        {
            var field = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 12, 8) };
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            field.Controls.Add(UiTheme.Label(caption, 9), 0, 0);
            field.Controls.Add(input, 0, 1);
            parent.Controls.Add(field, column, row);
        }

        private async Task ReloadAsync()
        {
            if (busy || IsDisposed) return;
            reload.Enabled = false;
            try { await LoadAccountsAsync(); }
            catch (Exception ex) { if (!IsDisposed) status.Text = ex.Message; }
            finally { if (!IsDisposed) reload.Enabled = true; }
        }

        private async Task LoadAccountsAsync()
        {
            var result = await AccountService.GetAccountsAsync();
            if (IsDisposed) return;
            accounts.DataSource = result.Select(account => new {
                Account = LoginIdentity.DisplayName(account.Email),
                DisplayName = account.DisplayName,
                Role = account.IsAdmin ? "Admin" : "Lễ tân",
                Status = account.IsActive ? "Hoạt động" : "Đã khóa"
            }).ToList();
            accounts.Columns["Account"].HeaderText = "Tài khoản";
            accounts.Columns["DisplayName"].HeaderText = "Tên hiển thị";
            accounts.Columns["Role"].HeaderText = "Quyền";
            accounts.Columns["Status"].HeaderText = "Trạng thái";
        }

        private async void CreateAdmin_Click(object sender, EventArgs e)
        {
            if (busy) return;
            if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(displayName.Text)
                || password.Text.Length < 6 || password.Text != confirmation.Text)
            {
                status.Text = "Điền tài khoản, tên hiển thị, mật khẩu ít nhất 6 ký tự và xác nhận trùng khớp.";
                return;
            }
            busy = true;
            create.Enabled = reload.Enabled = false;
            status.Text = "Đang tạo tài khoản...";
            try
            {
                await AccountService.CreateAdminAsync(new AdminRegistration {
                    UsernameOrEmail = username.Text.Trim(), DisplayName = displayName.Text.Trim(),
                    Password = password.Text, RequestId = Guid.NewGuid().ToString()
                });
                if (IsDisposed) return;
                status.Text = "Đã tạo admin. Tài khoản mới có thể đăng nhập ngay ở màn hình Login.";
                try { await LoadAccountsAsync(); }
                catch { if (!IsDisposed) status.Text += " Chưa tải được danh sách; bấm Tải lại sau."; }
            }
            catch (Exception ex) { if (!IsDisposed) status.Text = ex.Message; }
            finally
            {
                busy = false;
                if (!IsDisposed) { password.Clear(); confirmation.Clear(); create.Enabled = reload.Enabled = true; }
            }
        }
    }
}
