using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_CustomerDetails : UserControl
    {
        private readonly CloudScreen cloud;
        private int version;
        public UC_CustomerDetails()
        {
            InitializeComponent();
            guna2DataGridView1.ReadOnly=true; guna2DataGridView1.AllowUserToAddRows=false;
            txtSearchBy.SelectedIndex=0;
            cloud=new CloudScreen(this,"CUSTOMERS_CHANGED",LoadCustomersAsync,()=>guna2DataGridView1.DataSource=null);
        }
        private async Task LoadCustomersAsync()
        {
            int selected=version;
            string status=txtSearchBy.SelectedIndex==1?"active":txtSearchBy.SelectedIndex==2?"checkedout":"all";
            var result=await HotelServices.GetCustomersAsync(status);
            if(!IsDisposed && selected==version && HotelConnection.CurrentAccount!=null) guna2DataGridView1.DataSource=result;
        }
        private async void UC_CustomerDetails_Load(object sender,EventArgs e) { await cloud.RefreshAsync(); }
        private async void txtSearchBy_SelectedIndexChanged(object sender,EventArgs e) { ++version; if(cloud!=null) await cloud.RefreshAsync(); }
    }
}
