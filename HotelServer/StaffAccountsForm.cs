using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotelServer
{
    public sealed class StaffAccountsForm : Form
    {
        private readonly StaffAccountRepository accounts = new StaffAccountRepository();
        private readonly TextBox username = new TextBox { Name = "StaffUsername", Dock = DockStyle.Fill, MaxLength = 50 };
        private readonly TextBox displayName = new TextBox { Name = "StaffDisplayName", Dock = DockStyle.Fill, MaxLength = 100 };
        private readonly TextBox password = new TextBox { Name = "StaffPassword", Dock = DockStyle.Fill, MaxLength = 128, UseSystemPasswordChar = true };
        private readonly TextBox confirmPassword = new TextBox { Name = "StaffConfirmPassword", Dock = DockStyle.Fill, MaxLength = 128, UseSystemPasswordChar = true };
        private readonly Label status = new Label { Name = "StaffAccountStatus", Dock = DockStyle.Bottom, Height = 55, Padding = new Padding(10) };
        private readonly DataGridView list = new DataGridView
        {
            Name = "StaffAccounts", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };
        private readonly Button create = new Button { Name = "CreateStaff", Text = "Tạo tài khoản", AutoSize = true, Enabled = false };

        public StaffAccountsForm()
        {
            Text = "Tài khoản lễ tân";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(650, 500);
            MinimumSize = new Size(550, 450);
            StartPosition = FormStartPosition.CenterParent;
            var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddField(fields, "Tên đăng nhập", username, 0);
            AddField(fields, "Tên hiển thị", displayName, 1);
            AddField(fields, "Mật khẩu", password, 2);
            AddField(fields, "Nhập lại mật khẩu", confirmPassword, 3);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10) };
            var reload = new Button { Text = "Tải danh sách", AutoSize = true };
            actions.Controls.Add(create);
            actions.Controls.Add(reload);
            Controls.Add(list);
            Controls.Add(fields);
            Controls.Add(status);
            Controls.Add(actions);
            Shown += async delegate { await LoadAccountsAsync(); };
            reload.Click += async delegate { await LoadAccountsAsync(); };
            create.Click += async delegate
            {
                if (password.Text != confirmPassword.Text)
                {
                    status.Text = "Hai mật khẩu không khớp.";
                    return;
                }
                create.Enabled = false;
                try
                {
                    await accounts.CreateAsync(username.Text, displayName.Text, password.Text);
                    if (IsDisposed) return;
                    username.Clear();
                    displayName.Clear();
                    password.Clear();
                    confirmPassword.Clear();
                    await LoadAccountsAsync();
                    if (!IsDisposed) status.Text = "Đã tạo tài khoản lễ tân.";
                }
                catch (Exception ex) { if (!IsDisposed) status.Text = ex.Message; }
                finally { if (!IsDisposed) create.Enabled = true; }
            };
        }

        private async Task LoadAccountsAsync()
        {
            try
            {
                var result = await accounts.GetAccountsAsync();
                if (IsDisposed) return;
                list.DataSource = result;
                create.Enabled = true;
                status.Text = "Có " + result.Count + " tài khoản lễ tân đang hoạt động.";
            }
            catch (Exception ex) { if (!IsDisposed) status.Text = "Không thể tải tài khoản: " + ex.Message; }
        }

        private static void AddField(TableLayoutPanel fields, string caption, Control input, int row)
        {
            fields.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 6, 0, 8) }, 0, row);
            fields.Controls.Add(input, 1, row);
        }
    }
}
