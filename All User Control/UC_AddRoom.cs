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

        private async void btnAddRoom_Click(object sender, EventArgs e)
        {
            long price;
            if (string.IsNullOrWhiteSpace(txtRoomNo.Text) || string.IsNullOrWhiteSpace(txtRoomType.Text)
                || string.IsNullOrWhiteSpace(txtBed.Text) || !long.TryParse(txtPrice.Text, out price) || price < 0)
            {
                connectionStatus.Text = "Điền đầy đủ thông tin phòng và giá là số nguyên không âm.";
                return;
            }
            var room = new Room { roomNo = txtRoomNo.Text, roomType = txtRoomType.Text, bed = txtBed.Text, price = price };
            btnAddRoom.Enabled = false;
            try
            {
                await RoomService.AddRoomAsync(room);
                if (IsDisposed) return;
                clearAll();
                await LoadRoomsAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) connectionStatus.Text = "Thêm phòng thất bại: " + ex.Message;
            }
            finally
            {
                if (!IsDisposed) btnAddRoom.Enabled = true;
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
