using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace CTQLyKhachSan.ALL_User_Control
{
    public partial class UC_Employee : UserControl
    {
        private readonly CloudScreen cloud;
        public UC_Employee()
        {
            InitializeComponent();
            txtPassword.PasswordChar='*'; labelToSET.Text="Tự động";
            guna2DataGridView1.ReadOnly=guna2DataGridView2.ReadOnly=true;
            guna2DataGridView1.AllowUserToAddRows=guna2DataGridView2.AllowUserToAddRows=false;
            cloud=new CloudScreen(this,"EMPLOYEES_CHANGED",LoadEmployeesAsync,()=> { guna2DataGridView1.DataSource=null; guna2DataGridView2.DataSource=null; });
        }
        private async Task LoadEmployeesAsync()
        {
            var result=await HotelServices.GetEmployeesAsync();
            if(IsDisposed || HotelConnection.CurrentAccount==null) return;
            guna2DataGridView1.DataSource=result; guna2DataGridView2.DataSource=result;
        }
        private void label2_Click(object sender,EventArgs e) { }
        private void label10_Click(object sender,EventArgs e) { }
        private async void UC_Employee_Load(object sender,EventArgs e) { await cloud.RefreshAsync(); }
        private async void btnRegistation_Click(object sender,EventArgs e)
        {
            long mobile;
            if(!long.TryParse(txtMobile.Text,out mobile) || mobile<=0) { MessageBox.Show("Số điện thoại không hợp lệ."); return; }
            var employee=new EmployeeRegistration { ename=txtName.Text,mobile=mobile,gender=txtGender.Text,emailid=txtEmail.Text,username=txtUserName.Text,password=txtPassword.Text };
            await cloud.WriteAsync(btnRegistation,async()=> {
                int employeeId=await HotelServices.AddEmployeeAsync(employee);
                if(IsDisposed) return;
                clearAll(); MessageBox.Show("Đã thêm nhân viên mã "+employeeId+"."); await cloud.RefreshAsync();
            });
        }
        public void clearAll() { txtName.Clear(); txtMobile.Clear(); txtGender.SelectedIndex=-1; txtUserName.Clear(); txtEmail.Clear(); txtPassword.Clear(); }
        private async void tabEmployee_SelectedIndexChanged(object sender,EventArgs e) { if(cloud!=null) await cloud.RefreshAsync(); }
        private async void btnDelete_Click(object sender,EventArgs e)
        {
            int employeeId;
            if(!int.TryParse(txtID.Text,out employeeId) || employeeId<=0) { MessageBox.Show("Nhập mã nhân viên hợp lệ."); return; }
            if(MessageBox.Show("Xóa nhân viên mã "+employeeId+"?","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
            await cloud.WriteAsync(btnDelete,async()=> {
                await HotelServices.DeleteEmployeeAsync(employeeId);
                if(IsDisposed) return;
                txtID.Clear(); MessageBox.Show("Đã xóa nhân viên."); await cloud.RefreshAsync();
            });
        }
        private void UC_Employee_Leave(object sender,EventArgs e) { clearAll(); }
    }
}
