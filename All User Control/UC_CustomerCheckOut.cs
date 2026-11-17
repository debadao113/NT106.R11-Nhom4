using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_CustomerCheckOut : UserControl
    {
        function fn = new function();
        String query;
        int id; // Lưu cid của khách hàng khi click chọn trên bảng
        public UC_CustomerCheckOut()
        {
            InitializeComponent();
        }

        private void UC_CustomerCheckOut_Load(object sender, EventArgs e)
        {
            query = "select customer.cid, customer.cname, customer.mobile, customer.nationality, customer.gender, customer.dob, customer.idproof, customer.address, customer.checkin, rooms.roomNo, rooms.roomType, rooms.bed, rooms.price from customer inner join rooms on customer.roomid = rooms.roomid where chekout = 'NO'";
            DataSet ds = fn.GetData(query);
            guna2DataGridView1.DataSource = ds.Tables[0]; 
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            query = "select customer.cid, customer.cname, customer.mobile, customer.nationality, customer.gender, customer.dob, customer.idproof, customer.address, customer.checkin, rooms.roomNo, rooms.roomType, rooms.bed, rooms.price from customer inner join rooms on customer.roomid = rooms.roomid where cname like '" + txtName.Text + "%' and chekout = 'NO'";
            DataSet ds = fn.GetData(query);
            guna2DataGridView1.DataSource = ds.Tables[0];
        }

        private void guna2DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && guna2DataGridView1.Rows[e.RowIndex].Cells[0].Value != null)
             {
                id = int.Parse(guna2DataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString()); 
                txtCName.Text = guna2DataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString(); 
                txtRoom.Text = guna2DataGridView1.Rows[e.RowIndex].Cells[9].Value.ToString();
             }
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            if (txtCName.Text != "")
            {
                if (MessageBox.Show("Bạn có chắc chắn muốn thanh toán không?", "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    String cdate = txtCheckOutDate.Text;
                    query = "update customer set chekout = 'YES', checkout = '" + cdate + "' where cid = " + id + " update rooms set booked = 'NO' where roomNo = '" + txtRoom.Text + "'";
                    fn.setData(query, "Thanh toán thành công!");
                    UC_CustomerCheckOut_Load(this, null); 
                    clearAll();
                }
            }
            else
            {
                MessageBox.Show("Không có khách hàng để lựa chọn!", "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information); 
            }
        }
        public void clearAll()
        {
            txtCName.Clear();
            txtName.Clear();
            txtRoom.Clear();
            txtCheckOutDate.ResetText();
        }

        private void UC_CustomerCheckOut_Leave(object sender, EventArgs e)
        {
            clearAll();
        }
    }
}
