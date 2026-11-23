using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using HotelNetworking;

namespace HotelServer
{
    public sealed class ServerForm : Form
    {
        private readonly IPAddress listenAddress;
        private readonly int listenPort;
        private readonly bool validConfiguration;
        private readonly Label status;
        private readonly Button startButton;
        private readonly Button stopButton;
        private readonly TcpHotelServer server = new TcpHotelServer();
        private readonly ConcurrentQueue<string> pendingLogs = new ConcurrentQueue<string>();
        private readonly ListView clientList;
        private readonly ListBox logList;
        private readonly Timer refreshTimer;
        private bool closing;

        public ServerForm()
        {
            Text = "HotelServer - Quản lý kết nối khách sạn";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(800, 600);
            MinimumSize = new Size(650, 500);
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 9
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 9; i++)
                layout.RowStyles.Add(new RowStyle(i == 6 || i == 8 ? SizeType.Percent : SizeType.AutoSize, 50));

            AddWide(layout, CreateLabel("HotelServer", true), 0);
            layout.Controls.Add(CreateLabel("Địa chỉ lắng nghe"), 0, 1);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0"), 1, 1);
            layout.Controls.Add(CreateLabel("Cổng TCP"), 0, 2);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenPort"] ?? "8888"), 1, 2);
            status = CreateLabel("Server đang dừng.");
            status.Name = "ServerStatus";
            AddWide(layout, status, 3);
            startButton = new Button { Name = "StartServer", Text = "Khởi động", AutoSize = true };
            stopButton = new Button { Name = "StopServer", Text = "Dừng", AutoSize = true, Enabled = false };
            startButton.Click += StartServer;
            stopButton.Click += async delegate
            {
                stopButton.Enabled = false;
                status.Text = "Đang đóng các kết nối...";
                await server.StopAsync();
                status.Text = "Server đang dừng.";
                status.ForeColor = SystemColors.ControlText;
                startButton.Enabled = validConfiguration;
                RefreshMonitor();
            };
            validConfiguration = IPAddress.TryParse(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0", out listenAddress)
                && int.TryParse(ConfigurationManager.AppSettings["ListenPort"] ?? "8888", out listenPort)
                && listenPort >= 1 && listenPort <= 65535;
            if (!validConfiguration)
            {
                status.Text = "Cấu hình chưa hợp lệ. Kiểm tra ListenAddress và ListenPort trong HotelServer.exe.config.";
                status.ForeColor = Color.Firebrick;
                startButton.Enabled = false;
            }
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            actions.Controls.Add(startButton);
            actions.Controls.Add(stopButton);
            var accountButton = new Button { Text = "Tài khoản lễ tân", AutoSize = true };
            accountButton.Click += delegate
            {
                using (var dialog = new StaffAccountsForm()) dialog.ShowDialog(this);
            };
            actions.Controls.Add(accountButton);
            AddWide(layout, actions, 4);
            AddWide(layout, CreateLabel("Client đang kết nối"), 5);
            clientList = new ListView { Name = "ConnectedClients", View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
            clientList.Columns.Add("Mã kết nối", 260);
            clientList.Columns.Add("Tài khoản", 140);
            clientList.Columns.Add("Địa chỉ", 220);
            clientList.Columns.Add("Thời điểm", 180);
            AddWide(layout, clientList, 6);
            AddWide(layout, CreateLabel("Nhật ký kết nối"), 7);
            logList = new ListBox { Name = "ServerLog", Dock = DockStyle.Fill, HorizontalScrollbar = true };
            AddWide(layout, logList, 8);
            Controls.Add(layout);
            server.Logged += message =>
            {
                pendingLogs.Enqueue(message);
                string discarded;
                while (pendingLogs.Count > 500) pendingLogs.TryDequeue(out discarded);
            };
            refreshTimer = new Timer { Interval = 250 };
            refreshTimer.Tick += delegate { RefreshMonitor(); };
            refreshTimer.Start();
        }

        private void StartServer(object sender, EventArgs e)
        {
            try
            {
                server.Start(listenAddress, listenPort);
                status.Text = "Đang lắng nghe " + (TlsSettings.FromConfiguration().Enabled ? "TLS 1.2" : "TCP")
                    + " tại " + listenAddress + ":" + listenPort + ".";
                status.ForeColor = Color.DarkGreen;
                startButton.Enabled = false;
                stopButton.Enabled = true;
            }
            catch (SocketException ex)
            {
                status.Text = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                    ? "Không thể khởi động. Cổng " + listenPort + " đang được sử dụng. Hãy giải phóng cổng rồi thử lại."
                    : "Không thể khởi động TCP server: " + ex.Message;
                status.ForeColor = Color.Firebrick;
            }
        }

        private void RefreshMonitor()
        {
            clientList.BeginUpdate();
            clientList.Items.Clear();
            foreach (var client in server.GetClients())
                clientList.Items.Add(new ListViewItem(new[] { client.Id, client.Username ?? "Chưa đăng nhập", client.Endpoint, client.ConnectedAt.ToString("HH:mm:ss dd/MM") }));
            clientList.EndUpdate();
            string message;
            while (pendingLogs.TryDequeue(out message))
            {
                logList.Items.Add(message);
                if (logList.Items.Count > 500) logList.Items.RemoveAt(0);
                logList.TopIndex = logList.Items.Count - 1;
            }
            if (startButton.Enabled == false && stopButton.Enabled && !server.IsRunning)
            {
                stopButton.PerformClick();
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (!closing)
            {
                e.Cancel = true;
                startButton.Enabled = false;
                stopButton.Enabled = false;
                await server.StopAsync();
                closing = true;
                Close();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Dispose();
                server.StopAsync().GetAwaiter().GetResult();
            }
            base.Dispose(disposing);
        }

        private static void AddWide(TableLayoutPanel layout, Control control, int row)
        {
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private static Label CreateLabel(string text, bool title = false)
        {
            return new Label
            {
                Text = text, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 8),
                Font = new Font("Segoe UI", title ? 18F : 10F, title ? FontStyle.Bold : FontStyle.Regular)
            };
        }
    }
}
