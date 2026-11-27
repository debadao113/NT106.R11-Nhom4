using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan.Views.Controls
{
    public partial class RoomsControl : UserControl
    {
        private List<Room> rooms = new List<Room>();
        private readonly CloudScreen cloud;
        public RoomsControl()
        {
            InitializeComponent();
            UiTheme.StyleGrid(DataGridView1);
            cloud = new CloudScreen(this, "ROOMS_CHANGED", LoadRoomsAsync, ClearRooms);
            refreshButton.Click += async delegate
            {
                await cloud.RefreshAsync();
            };
            txtRoomSearch.TextChanged += delegate
            {
                ApplyFilter();
            };
        }

        private async Task LoadRoomsAsync()
        {
            var result = await RoomService.GetRoomsAsync();
            if (IsDisposed || HotelSession.CurrentAccount == null)
                return;
            rooms = result;
            ApplyFilter();
        }

        private void ClearRooms()
        {
            rooms.Clear();
            DataGridView1.DataSource = null;
        }

        private void ApplyFilter()
        {
            string term = txtRoomSearch.Text.Trim();
            DataGridView1.DataSource = rooms.Where(room => string.IsNullOrEmpty(term) || (room.Number + " " + room.Type + " " + room.Bed).IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
        }

        private async void RoomsControl_Load(object sender, EventArgs e)
        {
            await cloud.RefreshAsync();
        }

        private async void btnAddRoom_Click(object sender, EventArgs e)
        {
            long price;
            if (string.IsNullOrWhiteSpace(txtRoomNo.Text) || string.IsNullOrWhiteSpace(txtRoomType.Text) || string.IsNullOrWhiteSpace(txtBed.Text) || !long.TryParse(txtPrice.Text, out price) || price < 0)
            {
                pageStatus.Text = "Điền đầy đủ thông tin phòng và giá là số nguyên không âm.";
                return;
            }

            var room = new Room
            {
                Number = txtRoomNo.Text,
                Type = txtRoomType.Text,
                Bed = txtBed.Text,
                Price = price
            };
            await cloud.WriteAsync(btnAddRoom, async () =>
            {
                await RoomService.AddRoomAsync(room);
                if (IsDisposed)
                    return;
                ClearInputs();
                await cloud.RefreshAsync();
            });
        }

        private void ClearInputs()
        {
            txtRoomNo.Clear();
            txtRoomType.SelectedIndex = -1;
            txtBed.SelectedIndex = -1;
            txtPrice.Clear();
        }

        private void RoomsControl_Leave(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private async void RoomsControl_Enter(object sender, EventArgs e)
        {
            await cloud.RefreshAsync();
        }
    }
}
