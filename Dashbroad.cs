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
            uC_CustomerDetails1.Visible = false;
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
            panelMoving.Left = btnCheckOut.Left + 60; // Di chuyển thanh trượt chỉ mục
            uC_CustomerCheckOut1.Visible = true;
            uC_CustomerCheckOut1.BringToFront(); // Đưa giao diện thanh toán lên trên cùng
        }

        private void btnCustomerDetail_Click(object sender, EventArgs e)
        {
            panelMoving.Left = btnCustomerDetail .Left + 60;
            uC_CustomerDetails1.Visible = true;
            uC_CustomerDetails1.BringToFront();
        }
    }
}
