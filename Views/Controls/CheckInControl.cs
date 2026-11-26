using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan.Views.Controls
{
    public partial class CheckInControl : UserControl
    {
        private List<Room> rooms = new List<Room>();
        private readonly CloudScreen cloud;
        public CheckInControl()
        {
            InitializeComponent();
            txtDob.Value = DateTime.Today;
            txtCheckin.Value = DateTime.Today;
            cloud = new CloudScreen(this, "ROOMS_CHANGED", LoadRoomsAsync, () =>
            {
                rooms.Clear();
                txtRoomNo.Items.Clear();
                txtPrice.Clear();
            });
        }

        private async Task LoadRoomsAsync()
        {
            var result = await RoomService.GetRoomsAsync();
            if (IsDisposed || HotelSession.CurrentAccount == null)
                return;
            rooms = result;
            FillRooms();
        }

        private void FillRooms()
        {
            var selected = txtRoomNo.Text;
            txtRoomNo.Items.Clear();
            txtPrice.Clear();
            foreach (var room in rooms.Where(r => r.Bed == txtBed.Text && r.Type == txtRoom.Text && !r.IsOccupied))
                txtRoomNo.Items.Add(room.Number);
            if (txtRoomNo.Items.Contains(selected))
                txtRoomNo.SelectedItem = selected;
        }

        private async void CheckInControl_Load(object sender, EventArgs e)
        {
            await cloud.RefreshAsync();
        }

        private void txtBed_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtRoom.SelectedIndex = -1;
            FillRooms();
        }

        private async void txtRoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillRooms();
            if (cloud != null)
                await cloud.RefreshAsync();
        }

        private void txtRoomNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var room = rooms.FirstOrDefault(r => r.Number == txtRoomNo.Text);
            txtPrice.Text = room == null ? "" : room.Price.ToString();
        }

        private async void btnAlloteRoom_Click(object sender, EventArgs e)
        {
            long mobile;
            var room = rooms.FirstOrDefault(r => r.Number == txtRoomNo.Text);
            if (room == null || !long.TryParse(txtContact.Text, out mobile) || mobile <= 0)
            {
                MessageBox.Show("Chọn phòng trống và nhập số điện thoại hợp lệ.");
                return;
            }

            var customer = new CustomerRegistration
            {
                RoomId = room.Id,
                Name = txtName.Text,
                Phone = txtContact.Text.Trim(),
                Nationality = txtNationality.Text,
                Gender = txtGender.Text,
                BirthDate = txtDob.Value.ToString("yyyy-MM-dd"),
                IdentityDocument = txtIDProof.Text,
                Address = txtAddress.Text,
                CheckIn = txtCheckin.Value.ToString("yyyy-MM-dd")
            };
            await cloud.WriteAsync(btnAlloteRoom, async () =>
            {
                await HotelServices.RegisterCustomerAsync(customer);
                if (IsDisposed)
                    return;
                ClearInputs();
                MessageBox.Show("Đã đăng ký khách vào phòng " + room.Number + ".");
                await cloud.RefreshAsync();
            });
        }

        public void ClearInputs()
        {
            txtName.Clear();
            txtContact.Clear();
            txtNationality.Clear();
            txtGender.SelectedIndex = -1;
            txtDob.Value = DateTime.Today;
            txtIDProof.Clear();
            txtAddress.Clear();
            txtCheckin.Value = DateTime.Today;
            txtBed.SelectedIndex = -1;
            txtRoom.SelectedIndex = -1;
            txtRoomNo.Items.Clear();
            txtPrice.Clear();
        }

        private void CheckInControl_Leave(object sender, EventArgs e)
        {
            ClearInputs();
        }
    }
}
