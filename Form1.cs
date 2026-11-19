using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if(txtUsername.Text == "Long" && txtPassword.Text == "123")
            {
                labelError.Visible = false;
                Dashbroad ds = new Dashbroad();
                ds.FormClosed += delegate { Close(); };
                this.Hide();
                ds.Show();
            }
            else
            {
                labelError.Visible = true;
                txtPassword.Clear();

            }    
        }
    }
}
