using ChuuongTrinhQuanLyKhachSan;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTQLyKhachSan.ALL_User_Control
{
    public partial class UC_CustomerDetail : UserControl
    {
        function fn = new function();
        string query;
        private const string CheckedOut = "(ISNULL(customer.chekout,'NO') = 'YES' OR ISNULL(customer.checkout,'NO') NOT IN ('NO',''))";
        public UC_CustomerDetail()
        {
            InitializeComponent();
            guna2DataGridView1.ReadOnly = true;
            guna2DataGridView1.AllowUserToAddRows = false;
            VisibleChanged += delegate { if (Visible) { if (txtSearchBy.SelectedIndex < 0) txtSearchBy.SelectedIndex = 0; else txtSearchBy_SelectedIndexChanged(this, EventArgs.Empty); } };
        }

        private void UC_CustomerDetail_Load(object sender, EventArgs e)
        {

        }

        private void txtSearchBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(txtSearchBy.SelectedIndex == 0)
            {
                query = "select customer.cid, customer.cname, customer.mobile, customer.nationality, customer.gender, customer.dob, customer.idproof, customer.address, customer.checkin, rooms.roomNo, rooms.roomType, rooms.bed, rooms.price from customer inner join rooms on customer.roomid = rooms.roomid ";
                getRecord(query);
            }
            else if (txtSearchBy.SelectedIndex == 1)
            {
                query = "select customer.cid, customer.cname, customer.mobile, customer.nationality, customer.gender, customer.dob, customer.idproof, customer.address, customer.checkin, rooms.roomNo, rooms.roomType, rooms.bed, rooms.price from customer inner join rooms on customer.roomid = rooms.roomid  where NOT " + CheckedOut;
                getRecord(query);
            }
            else if (txtSearchBy.SelectedIndex == 2)
            {
                query = "select customer.cid, customer.cname, customer.mobile, customer.nationality, customer.gender, customer.dob, customer.idproof, customer.address, customer.checkin, rooms.roomNo, rooms.roomType, rooms.bed, rooms.price from customer inner join rooms on customer.roomid = rooms.roomid  where " + CheckedOut;
                getRecord(query);
            }
        }

        private void getRecord(string query)
        {
            DataSet ds = fn.GetData(query);
            guna2DataGridView1.DataSource = ds.Tables[0];
        }
    }
}
