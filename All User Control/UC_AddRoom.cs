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
            refreshButton = new Button
            {
                Text = "Tải lại phòng", AutoSize = true,
                Location = new Point(DataGridView1.Left, DataGridView1.Bottom + 12)
            };
            connectionStatus = new Label
            {
                Name = "RoomConnectionStatus", AutoSize = false,
                Location = new Point(DataGridView1.Left + 140, DataGridView1.Bottom + 12),
                Size = new Size(Math.Max(200, DataGridView1.Width - 140), 65)
            };
            refreshButton.Click += async delegate { await LoadRoomsAsync(); };
            Controls.Add(refreshButton);
            Controls.Add(connectionStatus);
            HotelConnection.Client.EventReceived += OnServerEvent;
            HotelConnection.Authenticated += OnConnected;
            HotelConnection.Client.Disconnected += OnDisconnected;
            Disposed += delegate
            {
                HotelConnection.Client.EventReceived -= OnServerEvent;
                HotelConnection.Authenticated -= OnConnected;
                HotelConnection.Client.Disconnected -= OnDisconnected;
            };
        }

        private void OnServerEvent(NetworkMessage message)
        {
            if (message.action == "ROOMS_CHANGED") OnConnected();
        }

        private void OnConnected()
        {
            PostToUi(async () => await LoadRoomsAsync());
        }

        private void OnDisconnected()
        {
            PostToUi(() =>
            {
                DataGridView1.DataSource = null;
                connectionStatus.Text = "Đã mất kết nối HotelServer. Nhấn Tải lại phòng để kết nối và lấy dữ liệu mới.";
            });
        }

        private void PostToUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); }
            catch (InvalidOperationException) { /* The control was disposed while scheduling the refresh. */ }
        }

        private async Task LoadRoomsAsync()
        {
            if (IsDisposed || Disposing) return;
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
                    if (!HotelConnection.Client.IsConnected)
                    {
                        OnDisconnected();
                        return;
                    }
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
