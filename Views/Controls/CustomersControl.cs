using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan.Views.Controls
{
    public partial class CustomersControl : UserControl
    {
        private readonly CloudScreen cloud;
        private int version;
        public CustomersControl()
        {
            InitializeComponent();
            UiTheme.StyleGrid(guna2DataGridView1);
            guna2DataGridView1.ReadOnly = true;
            guna2DataGridView1.AllowUserToAddRows = false;
            txtSearchBy.SelectedIndex = 0;
            cloud = new CloudScreen(this, "CUSTOMERS_CHANGED", LoadCustomersAsync, () => guna2DataGridView1.DataSource = null);
        }

        private async Task LoadCustomersAsync()
        {
            int selected = version;
            string status = txtSearchBy.SelectedIndex == 1 ? "active" : txtSearchBy.SelectedIndex == 2 ? "checkedout" : "all";
            var result = await HotelServices.GetCustomersAsync(status, txtNameFilter.Text.Trim());
            if (!IsDisposed && selected == version && HotelSession.CurrentAccount != null)
                guna2DataGridView1.DataSource = result;
        }

        private async void txtNameFilter_TextChanged(object sender, EventArgs e)
        {
            int request = ++version;
            await Task.Delay(300);
            if (!IsDisposed && request == version && cloud != null)
                await cloud.RefreshAsync();
        }

        private async void CustomersControl_Load(object sender, EventArgs e)
        {
            await cloud.RefreshAsync();
        }

        private async void txtSearchBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            ++version;
            if (cloud != null)
                await cloud.RefreshAsync();
        }
    }
}
