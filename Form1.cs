using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Text = "Đăng nhập - Quản lý khách sạn";
            MinimumSize = new Size(640, 480);
            ClientSize = new Size(guna2Panel1.Width + 48, guna2Panel1.Height + 48);
            AutoScroll = true;
            txtPassword.UseSystemPasswordChar = true;
            labelError.AutoSize = false;
            labelError.Location = new Point(txtUsername.Left, btnLogin.Bottom + 8);
            labelError.Size = new Size(txtUsername.Width, 40);
            label3.Top = labelError.Bottom + 10;
            guna2Panel1.Height = Math.Max(guna2Panel1.Height, label3.Bottom + 16);
            ClientSize = new Size(guna2Panel1.Width + 48, guna2Panel1.Height + 48);
            Resize += delegate { CenterLoginPanel(); };
            CenterLoginPanel();
        }

        private void CenterLoginPanel()
        {
            AutoScrollPosition = Point.Empty;
            guna2Panel1.Location = new Point(
                Math.Max(20, (ClientSize.Width - guna2Panel1.Width) / 2),
                Math.Max(20, (ClientSize.Height - guna2Panel1.Height) / 2));
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                labelError.Text = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                labelError.Visible = true;
                return;
            }
            btnLogin.Enabled = false;
            labelError.Text = "Đang xác thực qua HotelServer...";
            labelError.Visible = true;
            try
            {
                await HotelConnection.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);
                if (IsDisposed) return;
                labelError.Visible = false;
                txtPassword.Clear();
                Dashbroad ds = new Dashbroad();
                ds.FormClosed += delegate { Close(); };
                Hide();
                ds.Show();
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                labelError.Text = ex.Message;
                labelError.Visible = true;
                txtPassword.Clear();
            }
            finally
            {
                if (!IsDisposed) btnLogin.Enabled = true;
            }
        }
    }
}
