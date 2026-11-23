using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan.All_User_Control
{
    public partial class UC_CustomerRes : UserControl
    {
        private List<Room> rooms=new List<Room>();
        private readonly CloudScreen cloud;
        public UC_CustomerRes()
        {
            InitializeComponent();
            txtDob.Value=DateTime.Today; txtCheckin.Value=DateTime.Today;
            cloud=new CloudScreen(this,"ROOMS_CHANGED",LoadRoomsAsync,()=> { rooms.Clear(); txtRoomNo.Items.Clear(); txtPrice.Clear(); });
        }
        private async Task LoadRoomsAsync()
        {
            var result=await RoomService.GetRoomsAsync();
            if(IsDisposed || HotelConnection.CurrentAccount==null) return;
            rooms=result; FillRooms();
        }
        private void FillRooms()
        {
            var selected=txtRoomNo.Text;
            txtRoomNo.Items.Clear(); txtPrice.Clear();
            foreach(var room in rooms.Where(r=>r.bed==txtBed.Text && r.roomType==txtRoom.Text && string.Equals(r.booked ?? "NO","NO",StringComparison.OrdinalIgnoreCase)))
                txtRoomNo.Items.Add(room.roomNo);
            if(txtRoomNo.Items.Contains(selected)) txtRoomNo.SelectedItem=selected;
        }
        private async void UC_CustomerRes_Load(object sender,EventArgs e) { await cloud.RefreshAsync(); }
        private void txtBed_SelectedIndexChanged(object sender,EventArgs e) { txtRoom.SelectedIndex=-1; FillRooms(); }
        private async void txtRoom_SelectedIndexChanged(object sender,EventArgs e) { FillRooms(); if(cloud!=null) await cloud.RefreshAsync(); }
        private void txtRoomNo_SelectedIndexChanged(object sender,EventArgs e)
        {
            var room=rooms.FirstOrDefault(r=>r.roomNo==txtRoomNo.Text);
            txtPrice.Text=room==null?"":room.price.ToString();
        }
        private async void btnAlloteRoom_Click(object sender,EventArgs e)
        {
            long mobile;
            var room=rooms.FirstOrDefault(r=>r.roomNo==txtRoomNo.Text);
            if(room==null || !long.TryParse(txtContact.Text,out mobile) || mobile<=0)
            { MessageBox.Show("Chọn phòng trống và nhập số điện thoại hợp lệ."); return; }
            var customer=new CustomerRegistration { roomid=room.roomid,cname=txtName.Text,mobile=mobile,nationality=txtNationality.Text,
                gender=txtGender.Text,dob=txtDob.Value.ToString("yyyy-MM-dd"),idproof=txtIDProof.Text,address=txtAddress.Text,checkin=txtCheckin.Value.ToString("yyyy-MM-dd") };
            await cloud.WriteAsync(btnAlloteRoom,async()=> {
                await HotelServices.RegisterCustomerAsync(customer);
                if(IsDisposed) return;
                clearAll(); MessageBox.Show("Đã đăng ký khách vào phòng "+room.roomNo+"."); await cloud.RefreshAsync();
            });
        }
        public void clearAll()
        {
            txtName.Clear(); txtContact.Clear(); txtNationality.Clear(); txtGender.SelectedIndex=-1;
            txtDob.Value=DateTime.Today; txtIDProof.Clear(); txtAddress.Clear(); txtCheckin.Value=DateTime.Today;
            txtBed.SelectedIndex=-1; txtRoom.SelectedIndex=-1; txtRoomNo.Items.Clear(); txtPrice.Clear();
        }
        private void UC_CustomerRes_Leave(object sender,EventArgs e) { clearAll(); }
    }
}
