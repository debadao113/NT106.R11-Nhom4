using System;
using System.Drawing;
using System.Windows.Forms;
using HotelNetworking;

namespace ChuuongTrinhQuanLyKhachSan
{
    public sealed class ServerConnectionForm : Form
    {
        private readonly Label status = new Label { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(12) };
        private readonly Button connect = new Button { Text = "Kết nối / PING", AutoSize = true };
        private readonly Timer timer = new Timer { Interval = 500 };
        private bool busy;

        public ServerConnectionForm()
        {
            Text = "Kết nối HotelServer";
            ClientSize = new Size(560, 180);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(12) };
            var disconnect = new Button { Text = "Ngắt kết nối", AutoSize = true };
            disconnect.Click += delegate { HotelConnection.Client.Disconnect(); };
            connect.Click += async delegate
            {
                busy = true;
                connect.Enabled = false;
                status.Text = "Đang kết nối và kiểm tra...";
                try
                {
                    await HotelConnection.ConnectAsync();
                    var response = await HotelConnection.Client.SendAsync("PING", null);
                    if (!IsDisposed) status.Text = response.success == true ? "Đã kết nối. HotelServer phản hồi PING thành công." : response.error.message;
                }
                catch (Exception ex)
                {
                    if (!IsDisposed) status.Text = "Kết nối thất bại: " + ex.Message;
                }
                finally
                {
                    busy = false;
                    if (!IsDisposed) connect.Enabled = true;
                }
            };
            actions.Controls.Add(connect);
            actions.Controls.Add(disconnect);
            Controls.Add(actions);
            Controls.Add(status);
            status.Text = HotelConnection.Client.IsConnected ? "Đã kết nối HotelServer." : "Chưa kết nối HotelServer. Địa chỉ và cổng được cấu hình trong App.config.";
            timer.Tick += delegate
            {
                if (!busy && !HotelConnection.Client.IsConnected) status.Text = "Chưa kết nối hoặc đã mất kết nối HotelServer. Nhấn Kết nối / PING để thử lại.";
            };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
