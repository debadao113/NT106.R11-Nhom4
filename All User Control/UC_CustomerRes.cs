using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_CustomerRes : UserControl
    {
        function fn = new function();
        string query;
        int rid;
        public UC_CustomerRes()
        {
            InitializeComponent();
        }

        public void setcombobox(string query, ComboBox combo)
        {
            SqlDataReader sdr = fn.getForCombo(query);
            while(sdr.Read())
            {
                for(int i =0; i< sdr.FieldCount; i++)
                {
                    combo.Items.Add(sdr.GetString(i));
                }    
            }
            sdr.Close();
        }
        private void UC_CustomerRes_Load(object sender, EventArgs e)
        {

        }

        private void txtBed_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtRoom.SelectedIndex = -1;
            txtRoomNo.Items.Clear();
            txtPrice.Clear();
        }

        private void txtRoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtRoomNo.Items.Clear();
            query = "select roomNo from rooms where bed = '" + txtBed.Text + "' and roomType = '" + txtRoom.Text + "' and booked = 'NO'";
            setcombobox(query, txtRoomNo);

        }

        private void txtRoomNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            query = "select price, roomid from rooms where roomNo = '" + txtRoomNo.Text + "'";
            DataSet ds = fn.GetData(query);
            if (ds.Tables[0].Rows.Count > 0)
            {
                txtPrice.Text = ds.Tables[0].Rows[0][0].ToString(); // Lấy giá tiền gán vào txtPrice
                rid = int.Parse(ds.Tables[0].Rows[0][1].ToString()); // Lấy roomid lưu vào biến rid
            }
        }

        private void btnAlloteRoom_Click(object sender, EventArgs e)
        {
            if (txtName.Text != "" && txtContact.Text != "" && txtNationality.Text != "" && txtGender.Text != "" && txtDob.Text != "" && txtIDProof.Text != "" && txtAddress.Text != "" && txtCheckin.Text != "" && txtPrice.Text != "")
            {
                String cname = txtName.Text;
                Int64 mobile = Int64.Parse(txtContact.Text);
                String national = txtNationality.Text;
                String gender = txtGender.Text;
                String dob = txtDob.Text;
                String idproof = txtIDProof.Text;
                String address = txtAddress.Text;
                String checkin = txtCheckin.Text;

                // Lưu thông tin khách hàng và cập nhật phòng sang trạng thái đã đặt (booked = 'YES')
                query = "insert into customer (cname, mobile, nationality, gender, dob, idproof, address, checkin, roomid) values ('" + cname + "', " + mobile + ", '" + national + "', '" + gender + "', '" + dob + "', '" + idproof + "', '" + address + "', '" + checkin + "', " + rid + ") update rooms set booked = 'YES' where roomNo = '" + txtRoomNo.Text + "'";

                fn.setData(query, "Số phòng " + txtRoomNo.Text + " đăng ký khách hàng thành công!");
                clearAll();
            }
            else
            {
                MessageBox.Show("Xin vui lòng nhập đầy đủ thông tin!", "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public void clearAll()
        {
            txtName.Clear();
            txtContact.Clear();
            txtNationality.Clear();
            txtGender.SelectedIndex = -1;
            txtDob.ResetText();
            txtIDProof.Clear();
            txtAddress.Clear();
            txtCheckin.ResetText();
            txtBed.SelectedIndex = -1;
            txtRoom.SelectedIndex = -1;
            txtRoomNo.Items.Clear();
            txtPrice.Clear();
        }

        private void UC_CustomerRes_Leave(object sender, EventArgs e)
        {
            clearAll();
        }
    }
}
