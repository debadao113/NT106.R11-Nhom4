using System.Data.SqlClient;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChuuongTrinhQuanLyKhachSan;
namespace CTQLyKhachSan.ALL_User_Control
{
    public partial class UC_Employee : UserControl
    {
        function fn = new function();
        string query;
        public UC_Employee()
        {
            InitializeComponent();
            txtPassword.PasswordChar = '*';
            guna2DataGridView1.ReadOnly = guna2DataGridView2.ReadOnly = true;
            guna2DataGridView1.AllowUserToAddRows = guna2DataGridView2.AllowUserToAddRows = false;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void UC_Employee_Load(object sender, EventArgs e)
        {
            getMaxId();
        }
        public void getMaxId()
        {
            query = "select ISNULL(max(eid),0) from employee";
            DataSet ds = fn.GetData(query);
            if (ds.Tables[0].Rows[0][0].ToString() != "")
            {
                Int64 num = Int64.Parse(ds.Tables[0].Rows[0][0].ToString());
                labelToSET.Text = (num + 1).ToString();
            }
        }

        private void btnRegistation_Click(object sender, EventArgs e)
        {
            long mobile;
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtMobile.Text) ||
                string.IsNullOrWhiteSpace(txtGender.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Text) ||
                !long.TryParse(txtMobile.Text, out mobile) || mobile <= 0)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin và số điện thoại hợp lệ.");
                return;
            }
            try
            {
                string password;
                using (var hash = new Rfc2898DeriveBytes(txtPassword.Text, 16, 210000, HashAlgorithmName.SHA256))
                    password = "PBKDF2$210000$" + Convert.ToBase64String(hash.Salt) + "$" + Convert.ToBase64String(hash.GetBytes(32));
                fn.setData("INSERT INTO employee (ename,mobile,gender,emailid,username,pass) VALUES (@name,@mobile,@gender,@email,@username,@pass)",
                    "Đã thêm nhân viên.", new SqlParameter("@name", txtName.Text.Trim()), new SqlParameter("@mobile", mobile),
                    new SqlParameter("@gender", txtGender.Text), new SqlParameter("@email", txtEmail.Text.Trim()),
                    new SqlParameter("@username", txtUserName.Text.Trim()), new SqlParameter("@pass", password));
                clearAll();
                getMaxId();
            }
            catch (SqlException ex) { MessageBox.Show(ex.Number == 2627 || ex.Number == 2601 ? "Tên tài khoản đã tồn tại." : "Không thể lưu nhân viên: " + ex.Message); }
        }
        public void clearAll()
        {
            txtName.Clear();
            txtMobile.Clear();
            txtGender.SelectedIndex = -1;
            txtUserName.Clear();
            txtEmail.Clear();
            txtPassword.Clear();
        }

        private void tabEmployee_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabEmployee.SelectedIndex == 1)
            {
                setEmployee(guna2DataGridView1);
            }
            else if (tabEmployee.SelectedIndex == 2)
            {
                setEmployee(guna2DataGridView2);
            }
        }
        public void setEmployee(DataGridView dgv)
        {
            query = "SELECT eid,ename,mobile,gender,emailid,username FROM employee";
            DataSet ds = fn.GetData(query);
            dgv.DataSource = ds.Tables[0];
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int employeeId;
            if (int.TryParse(txtID.Text, out employeeId) && employeeId > 0)
            {
                if (MessageBox.Show("Are You Sure", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    query = "DELETE FROM employee WHERE eid = @id";
                    fn.setData(query, "Information Employee deleted", new SqlParameter("@id", employeeId));
                    tabEmployee_SelectedIndexChanged(this, null);
                }
            }
        }

        private void UC_Employee_Leave(object sender, EventArgs e)
        {
            clearAll();
        }
    }
}
