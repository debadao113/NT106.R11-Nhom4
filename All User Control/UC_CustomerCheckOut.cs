using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_CustomerCheckOut : UserControl
    {
        private int id, searchVersion;
        private readonly CloudScreen cloud;
        public UC_CustomerCheckOut()
        {
            InitializeComponent();
            guna2DataGridView1.ReadOnly=true; guna2DataGridView1.AllowUserToAddRows=false;
            txtCheckOutDate.Value=DateTime.Today;
            cloud=new CloudScreen(this,"CUSTOMERS_CHANGED",LoadCustomersAsync,()=> { guna2DataGridView1.DataSource=null; ClearSelection(); });
        }
        private async Task LoadCustomersAsync()
        {
            int version=searchVersion;
            var result=await HotelServices.GetCustomersAsync("active",txtName.Text);
            if(IsDisposed || version!=searchVersion || HotelConnection.CurrentAccount==null) return;
            ClearSelection(); guna2DataGridView1.DataSource=result;
        }
        private async void UC_CustomerCheckOut_Load(object sender,EventArgs e) { await cloud.RefreshAsync(); }
        private async void txtName_TextChanged(object sender,EventArgs e)
        {
            int version=++searchVersion;
            await Task.Delay(300);
            if(!IsDisposed && version==searchVersion && cloud!=null) await cloud.RefreshAsync();
        }
        private void guna2DataGridView1_CellContentClick(object sender,DataGridViewCellEventArgs e)
        {
            if(e.RowIndex<0) return;
            var customer=guna2DataGridView1.Rows[e.RowIndex].DataBoundItem as CustomerRecord;
            if(customer==null) return;
            id=customer.cid; txtCName.Text=customer.cname; txtRoom.Text=customer.roomNo;
        }
        private async void btnCheckOut_Click(object sender,EventArgs e)
        {
            if(id<=0) { MessageBox.Show("Vui lòng chọn khách hàng cần trả phòng."); return; }
            if(MessageBox.Show("Xác nhận trả phòng cho khách đã chọn?","Xác nhận",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK) return;
            int selected=id; string date=txtCheckOutDate.Value.ToString("yyyy-MM-dd");
            await cloud.WriteAsync(btnCheckOut,async()=> {
                await HotelServices.CheckoutAsync(selected,date);
                if(IsDisposed) return;
                clearAll(); MessageBox.Show("Đã trả phòng thành công."); await cloud.RefreshAsync();
            });
        }
        private void ClearSelection() { id=0; txtCName.Clear(); txtRoom.Clear(); }
        public void clearAll() { ClearSelection(); txtName.Clear(); txtCheckOutDate.Value=DateTime.Today; }
        private void UC_CustomerCheckOut_Leave(object sender,EventArgs e) { clearAll(); }
    }
}
