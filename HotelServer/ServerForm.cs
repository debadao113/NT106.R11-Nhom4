using System;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace HotelServer
{
    public sealed class ServerForm : Form
    {
        private readonly IPAddress listenAddress;
        private readonly int listenPort;
        private readonly Label status;
        private readonly Button startButton;
        private readonly Button stopButton;
        private TcpListener listener;

        public ServerForm()
        {
            Text = "HotelServer - Quản lý kết nối khách sạn";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(650, 330);
            MinimumSize = new Size(600, 360);
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var title = CreateLabel("HotelServer", true);
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 2);

            layout.Controls.Add(CreateLabel("Địa chỉ lắng nghe"), 0, 1);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0"), 1, 1);
            layout.Controls.Add(CreateLabel("Cổng TCP"), 0, 2);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenPort"] ?? "8888"), 1, 2);

            status = CreateLabel("Server đang dừng.");
            status.Name = "ServerStatus";
            startButton = new Button { Name = "StartServer", Text = "Khởi động", AutoSize = true };
            stopButton = new Button { Name = "StopServer", Text = "Dừng", AutoSize = true, Enabled = false };
            startButton.Click += StartServer;
            stopButton.Click += delegate { StopServer(); };

            if (!IPAddress.TryParse(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0", out listenAddress)
                || !int.TryParse(ConfigurationManager.AppSettings["ListenPort"] ?? "8888", out listenPort)
                || listenPort < 1 || listenPort > 65535)
            {
                status.Text = "Cấu hình chưa hợp lệ. Kiểm tra ListenAddress và ListenPort trong HotelServer.exe.config.";
                status.ForeColor = Color.Firebrick;
                startButton.Enabled = false;
            }

            layout.Controls.Add(status, 0, 3);
            layout.SetColumnSpan(status, 2);

            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            actions.Controls.Add(startButton);
            actions.Controls.Add(stopButton);
            layout.Controls.Add(actions, 0, 4);
            layout.SetColumnSpan(actions, 2);
            Controls.Add(layout);
        }

        private void StartServer(object sender, EventArgs e)
        {
            if (listener != null)
            {
                return;
            }

            var candidate = new TcpListener(listenAddress, listenPort);
            try
            {
                candidate.ExclusiveAddressUse = true;
                candidate.Start();
                listener = candidate;
                status.Text = "Đang lắng nghe TCP tại " + listener.LocalEndpoint + ".";
                status.ForeColor = Color.DarkGreen;
                startButton.Enabled = false;
                stopButton.Enabled = true;
            }
            catch (SocketException ex)
            {
                candidate.Stop();
                status.Text = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                    ? "Không thể khởi động. Cổng " + listenPort + " đang được sử dụng. Hãy dừng ứng dụng đang giữ cổng rồi thử lại."
                    : "Không thể khởi động TCP server: " + ex.Message;
                status.ForeColor = Color.Firebrick;
            }
        }

        private void StopServer()
        {
            if (listener == null)
            {
                return;
            }

            listener.Stop();
            listener = null;
            status.Text = "Server đang dừng.";
            status.ForeColor = SystemColors.ControlText;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopServer();
            }

            base.Dispose(disposing);
        }

        private static Label CreateLabel(string text, bool title = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 12),
                Font = new Font("Segoe UI", title ? 18F : 10F, title ? FontStyle.Bold : FontStyle.Regular)
            };
        }
    }
}
