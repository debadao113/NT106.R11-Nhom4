using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan.Views.Controls
{
    public partial class EmployeesControl : UserControl
    {
        private readonly CloudScreen cloud;
        public EmployeesControl()
        {
            InitializeComponent();
            UiTheme.StyleGrid(guna2DataGridView1);
            UiTheme.StyleTabs(tabControl1);
            tabControl1.SelectedIndexChanged += tabEmployee_SelectedIndexChanged;
            UiTheme.StyleGrid(guna2DataGridView3);
            labelToSET.Text = "Tự động";
            guna2DataGridView1.ReadOnly = guna2DataGridView3.ReadOnly = true;
            guna2DataGridView1.AllowUserToAddRows = guna2DataGridView3.AllowUserToAddRows = false;
            cloud = new CloudScreen(this, "EMPLOYEES_CHANGED", LoadEmployeesAsync, () =>
            {
                guna2DataGridView1.DataSource = null;
                guna2DataGridView3.DataSource = null;
            });
        }

        private async Task LoadEmployeesAsync()
        {
            var result = await HotelServices.GetEmployeesAsync();
            if (IsDisposed || HotelSession.CurrentAccount == null)
                return;
            guna2DataGridView1.DataSource = result;
            guna2DataGridView3.DataSource = result;
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void label10_Click(object sender, EventArgs e)
        {
        }

        private async void EmployeesControl_Load(object sender, EventArgs e)
        {
            await cloud.RefreshAsync();
        }

        private async void btnRegistation_Click(object sender, EventArgs e)
        {
            long mobile;
            if (!long.TryParse(txtMobile.Text, out mobile) || mobile <= 0)
            {
                MessageBox.Show("Số điện thoại không hợp lệ.");
                return;
            }

            var employee = new EmployeeRegistration
            {
                Name = txtName.Text,
                Phone = txtMobile.Text.Trim(),
                Gender = txtGender.Text,
                Email = txtEmail.Text
            };
            await cloud.WriteAsync(btnRegistation, async () =>
            {
                int employeeId = await HotelServices.AddEmployeeAsync(employee);
                if (IsDisposed)
                    return;
                ClearInputs();
                MessageBox.Show("Đã thêm nhân viên mã " + employeeId + ".");
                await cloud.RefreshAsync();
            });
        }

        public void ClearInputs()
        {
            txtName.Clear();
            txtMobile.Clear();
            txtGender.SelectedIndex = -1;
            txtEmail.Clear();
        }

        private async void tabEmployee_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cloud != null)
                await cloud.RefreshAsync();
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            int employeeId;
            if (!int.TryParse(txtID.Text, out employeeId) || employeeId <= 0)
            {
                MessageBox.Show("Nhập mã nhân viên hợp lệ.");
                return;
            }

            if (MessageBox.Show("Xóa nhân viên mã " + employeeId + "?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            await cloud.WriteAsync(btnDelete, async () =>
            {
                await HotelServices.DeleteEmployeeAsync(employeeId);
                if (IsDisposed)
                    return;
                txtID.Clear();
                MessageBox.Show("Đã xóa nhân viên.");
                await cloud.RefreshAsync();
            });
        }

        private void employeeDeleteGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            var employee = guna2DataGridView3.Rows[e.RowIndex].DataBoundItem as EmployeeRecord;
            if (employee != null)
                txtID.Text = employee.Id.ToString();
        }

        private void EmployeesControl_Leave(object sender, EventArgs e)
        {
            ClearInputs();
        }
    }
}
