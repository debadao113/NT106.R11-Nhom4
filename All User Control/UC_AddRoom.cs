using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_AddRoom : UserControl
    {
        private readonly Label connectionStatus;
        private readonly Button refreshButton;
        private bool loading;
        private bool reloadRequested;
        function fn = new function();
        string query;

        public UC_AddRoom()
        {
            InitializeComponent();
            DataGridView1.ReadOnly = true;
            DataGridView1.AllowUserToAddRows = false;
            refreshButton = new Button { Text = "Tải lại phòng", AutoSize = true, Location = new Point(33, 680) };
            connectionStatus = new Label { AutoSize = false, Location = new Point(180, 680), Size = new Size(680, 65) };
            refreshButton.Click += async delegate { await LoadRoomsAsync(); };
            Controls.Add(refreshButton);
            Controls.Add(connectionStatus);
        }

        private async Task LoadRoomsAsync()
        {
            reloadRequested = true;
            if (loading) return;
            loading = true;
            refreshButton.Enabled = false;
            try
            {
                do
                {
                    reloadRequested = false;
                    connectionStatus.Text = "Đang tải phòng từ HotelServer...";
                    var rooms = await RoomService.GetRoomsAsync();
                    if (IsDisposed) return;
                    DataGridView1.DataSource = rooms;
                    connectionStatus.Text = "Đã tải " + rooms.Count + " phòng từ HotelServer.";
                } while (reloadRequested);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) connectionStatus.Text = "Không thể tải phòng: " + ex.Message + " Nhấn Tải lại phòng để thử lại.";
            }
            finally
            {
                loading = false;
                if (!IsDisposed) refreshButton.Enabled = true;
            }
        }

        private async void UC_AddRoom_Load(object sender, EventArgs e)
        {
            await LoadRoomsAsync();
        }

        private void btnAddRoom_Click(object sender, EventArgs e)
        {
            if (txtRoomNo.Text != "" && txtRoomType.Text != "" && txtBed.Text != "" && txtPrice.Text != "")
            {
                string roomno = txtRoomNo.Text;
                string type = txtRoomType.Text;
                string bed = txtBed.Text;
                Int64 price = Int64.Parse(txtPrice.Text);
                query = "insert into rooms (roomNo, roomType, bed, price) values ('" + roomno + "','" + type + "','" + bed + "','" + price + "')";
                fn.setData(query, "Đã thêm phòng");
                UC_AddRoom_Load(this, null);
                clearAll();
            }
            else
            {
                MessageBox.Show("Xin vui lòng điền đầy đủ thông tin", "Warning !", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void clearAll()
        {
            txtRoomNo.Clear();
            txtRoomType.SelectedIndex = -1;
            txtBed.SelectedIndex = -1;
            txtPrice.Clear();
        }

        private void UC_AddRoom_Leave(object sender, EventArgs e) { clearAll(); }
        private async void UC_AddRoom_Enter(object sender, EventArgs e) { await LoadRoomsAsync(); }
    }
}
