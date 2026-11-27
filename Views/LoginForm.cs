using System;
using System.Drawing;
using System.Windows.Forms;
using HotelData;

namespace ChuuongTrinhQuanLyKhachSan
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
            txtUsername.MaxLength = 254;
            Shown += delegate
            {
                txtUsername.Focus();
            };
            showPassword.CheckedChanged += delegate
            {
                txtPassword.UseSystemPasswordChar = !showPassword.Checked;
            };
            KeyPreview = true;
            KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && btnLogin.Enabled)
                {
                    e.SuppressKeyPress = true;
                    btnLogin.PerformClick();
                }
            };
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                labelError.Text = "Vui lòng nhập tên đăng nhập hoặc email và mật khẩu.";
                labelError.ForeColor = Color.FromArgb(190, 75, 75);
                labelError.Visible = true;
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";
            labelError.ForeColor = Color.FromArgb(0, 120, 212);
            labelError.Text = "Đang xác thực qua Supabase...";
            labelError.Visible = true;
            try
            {
                await HotelSession.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);
                if (IsDisposed)
                    return;
                labelError.Visible = false;
                txtPassword.Clear();
                DashboardForm ds = new DashboardForm();
                ds.FormClosed += delegate
                {
                    if (ds.LoggedOut)
                    {
                        Show();
                        txtPassword.Focus();
                    }
                    else
                        Close();
                    ds.Dispose();
                };
                Hide();
                ds.Show();
            }
            catch (Exception ex)
            {
                if (IsDisposed)
                    return;
                labelError.Text = ex.Message;
                labelError.ForeColor = Color.FromArgb(190, 75, 75);
                labelError.Visible = true;
                txtPassword.Clear();
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                }
            }
        }
    }
}
