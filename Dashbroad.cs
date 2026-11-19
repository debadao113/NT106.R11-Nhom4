using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CTQLyKhachSan.ALL_User_Control;

namespace ChuuongTrinhQuanLyKhachSan
{
    public partial class Dashbroad : Form
    {
        private UC_Employee uC_Employee1;

        public Dashbroad()
        {
            InitializeComponent();

            uC_Employee1 = new UC_Employee
            {
                AutoScaleMode = AutoScaleMode.None,
                BackColor = Color.White,
                Location = new Point(0, 0),
                Name = "uC_Employee1",
                Size = new Size(1858, 794),
                Visible = false
            };
            panel2.Controls.Add(uC_Employee1);
            btnEmployee.Click += btnEmployee_Click;

            var connectionButton = new Button
            {
                Text = "Kết nối server",
                AutoSize = true,
                Location = new Point(20, ClientSize.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            connectionButton.Click += delegate
            {
                using (var dialog = new ServerConnectionForm()) dialog.ShowDialog(this);
            };
            Controls.Add(connectionButton);
            connectionButton.BringToFront();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Dashbroad_Load(object sender, EventArgs e)
        {
            uC_AddRoom1.Visible = false;
            uC_CustomerRes1.Visible = false;
            uC_CustomerCheckOut1.Visible = false;
            uC_Employee1.Visible = false;
            btnAddRoom.PerformClick();
        }

        private void btnAddRoom_Click(object sender, EventArgs e)
        {
            panelMoving.Left = btnAddRoom.Left + 50;
            uC_AddRoom1.Visible = true;
            uC_AddRoom1.BringToFront();
        }

        private void btnCustomerRes_Click(object sender, EventArgs e)
        {
            panelMoving.Left = btnCustomerRes.Left + 60;
            uC_CustomerRes1.Visible = true;
            uC_CustomerRes1.BringToFront();
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            panelMoving.Left = btnCheckOut.Left + 60;
            uC_CustomerCheckOut1.Visible = true;
            uC_CustomerCheckOut1.BringToFront();
        }

        private void btnEmployee_Click(object sender, EventArgs e)
        {
            panelMoving.Left = btnEmployee.Left + 60;
            uC_Employee1.Visible = true;
            uC_Employee1.BringToFront();
        }
    }
}