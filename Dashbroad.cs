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
    public partial class Dashbroad : Form
    {
        public Dashbroad()
        {
            InitializeComponent();
            guna2Elipse1.Dispose();
            guna2Elipse2.Dispose();
            guna2Elipse3.Dispose();
            Region = null;
            SuspendLayout();
            UpdateAccountTitle();
            MinimumSize = new Size(640, 480);
            ClientSize = new Size(1100, 720);
            btnExit.Visible = false;
            btnMinisize.Visible = false;
            panelMoving.Visible = false;

            var navigation = new FlowLayoutPanel
            {
                Name = "Navigation", Dock = DockStyle.Top, AutoSize = true,
                WrapContents = true, Padding = new Padding(8)
            };
            foreach (var button in new[] { btnAddRoom, btnCustomerRes, btnCheckOut, btnCustomerDetail, btnEmployee })
            {
                button.Size = new Size(170, 54);
                button.Margin = new Padding(4);
                navigation.Controls.Add(button);
            }
            Controls.Remove(panel1);
            panel1.Dispose();
            Controls.Add(navigation);
            panel2.Dock = DockStyle.Fill;
            panel2.AutoScroll = true;
            foreach (Control page in panel2.Controls)
            {
                page.Location = Point.Empty;
                page.Visible = false;
            }

            var connectionButton = new Button
            {
                Name = "ServerConnection", Text = "Kết nối server", AutoSize = true
            };
            connectionButton.Click += delegate
            {
                using (var dialog = new ServerConnectionForm()) dialog.ShowDialog(this);
                UpdateAccountTitle();
            };
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8)
            };
            footer.Controls.Add(connectionButton);
            Controls.Add(footer);
            Controls.SetChildIndex(panel2, 0);
            ResumeLayout(true);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Dashbroad_Load(object sender, EventArgs e)
        {
            uC_AddRoom1.Visible = false;
            uC_CustomerRes1.Visible = false;
            uC_CustomerCheckOut1.Visible = false;
            btnAddRoom.PerformClick();
        }
        private void btnAddRoom_Click(object sender, EventArgs e)
        {
            ShowPage(uC_AddRoom1);

        }

        private void btnCustomerRes_Click(object sender, EventArgs e)
        {
            ShowPage(uC_CustomerRes1);
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            ShowPage(uC_CustomerCheckOut1);
        }

        private void ShowPage(Control selectedPage)
        {
            panel2.SuspendLayout();
            panel2.AutoScrollPosition = Point.Empty;
            foreach (Control page in panel2.Controls) page.Visible = page == selectedPage;
            selectedPage.Location = Point.Empty;
            panel2.AutoScrollMinSize = selectedPage.Size;
            selectedPage.BringToFront();
            panel2.ResumeLayout(true);
        }

        private void UpdateAccountTitle()
        {
            Text = "Lễ tân - Quản lý khách sạn";
            var account = HotelNetworking.HotelConnection.CurrentAccount;
            if (account != null) Text += " - " + account.username;
        }
    }
}
